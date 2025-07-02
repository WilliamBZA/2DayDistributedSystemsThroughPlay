namespace SimonSays;

using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SimonSays.Hubs;
using System.Text;
using System.Text.Json;
using static SimonSays.Pages.IndexModel;

public class MessageBusService : BackgroundService
{
    private ServiceBusClient serviceBusClient;
    private ServiceBusProcessor processor;
    private Dictionary<Type, string> messageDestinations = new Dictionary<Type, string>();
    private Dictionary<string, ServiceBusSender> senders = new Dictionary<string, ServiceBusSender>();
    private readonly Dictionary<Type, Delegate> typeActionMaps = new Dictionary<Type, Delegate>();
    private readonly IHubContext<EventsHub> hubContext;
    private readonly ILogger<MessageBusService> logger;
    private string serviceBusConnectionString;
    private readonly string queueName = "Puzzle_progress";

    public MessageBusService(IHubContext<EventsHub> hubContext, ILogger<MessageBusService> logger)
    {
        this.hubContext = hubContext;
        this.logger = logger;

        serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString")!;

        serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        processor = serviceBusClient.CreateProcessor(queueName);
        processor.ProcessErrorAsync += ProcessErrorAsync;
        processor.ProcessMessageAsync += ProcessMessageAsync;
    }

    public void On<T>(Action<T> handler)
    {
        typeActionMaps[typeof(T)] = handler;
    }

    public void Route<T>(string destination)
    {
        var type = typeof(T);

        messageDestinations[type] = destination;

        if (!senders.ContainsKey(destination))
        {
            var sender = serviceBusClient.CreateSender(destination);
            senders.Add(destination, sender);
        }
    }

    public async Task SendAsync<T>(T message)
    {
        var messageType = typeof(T).FullName;

        var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(message))
        {
            ApplicationProperties = { ["messagetype"] = messageType },
            ContentType = "text/json"
        };

        var sender = GetSender<T>();
        await sender.SendMessageAsync(serviceBusMessage);
    }

    private ServiceBusSender GetSender<T>()
    {
        var destination = messageDestinations[typeof(T)];
        return senders[destination];
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return processor.StartProcessingAsync(stoppingToken);        
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        if (!args.Message.ApplicationProperties.TryGetValue("messagetype", out var messageTypeObj))
        {
            return;
        }

        var messageType = messageTypeObj as string;
        if (messageType is null)
        {
            throw new ArgumentNullException("Message type is null or not set in application properties.");
        }

        string payload = GetMessageBody(args.Message);

        var evt = new
        {
            EventType = messageType,
            Timestamp = args.Message.EnqueuedTime.UtcDateTime,
            Payload = payload
        };

        await hubContext.Clients.All.SendAsync("ReceiveEvent", evt.EventType, evt.Timestamp, evt.Payload);

        // Find and invoke the typed handler if it exists
        if (GetTypeByName(messageType) is Type type)
        {
            if (typeActionMaps.TryGetValue(type, out var handler))
            {
                try
                {
                    var typedPayload = JsonSerializer.Deserialize(payload, type);
                    if (typedPayload != null)
                    {
                        handler.DynamicInvoke(typedPayload);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error deserializing or invoking handler for message type {MessageType}", messageType);
                }
            }
        }
    }

    private static string GetMessageBody(ServiceBusReceivedMessage message)
    {
        var amqpMessage = message.GetRawAmqpMessage();

        if (amqpMessage.Body.TryGetValue(out var value))
        {
            return value?.ToString() ?? string.Empty;
        }
        else if (amqpMessage.Body.TryGetSequence(out IEnumerable<IList<object>>? sequence) && sequence != null)
        {
            return string.Join("\n", sequence.Select(seq => string.Join(", ", seq)));
        }
        else if (amqpMessage.Body.TryGetData(out IEnumerable<ReadOnlyMemory<byte>>? dataSections) && dataSections != null)
        {
            var combinedBytes = dataSections.SelectMany(b => b.ToArray()).ToArray();
            return Encoding.UTF8.GetString(combinedBytes);
        }

        return string.Empty;
    }

    private async Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Error processing message");
        await Task.CompletedTask;
    }

    private Type? GetTypeByName(string typeName)
    {
        // Try exact match first
        var type = Type.GetType(typeName);
        if (type != null) return type;

        // Search all loaded assemblies
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = asm.GetType(typeName);
            if (type != null) return type;
        }
        return null;
    }

    public override async void Dispose()
    {
        await processor.StopProcessingAsync();
        await processor.DisposeAsync();
        await serviceBusClient.DisposeAsync();

        base.Dispose();
    }
}
