using Azure.Messaging.ServiceBus;
using MessagingHelper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using PuzzleController.Messages;
using Puzzle.Messages;
using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;

namespace PuzzleController.Services;

public class MessageBusService : BackgroundService
{
    private readonly ILogger<MessageBusService> logger;
    private readonly ServiceBusClient client;
    private readonly MessageProcessor messageProcessor;
    private readonly ServiceBusHelper helper;
    private ServiceBusProcessor processor;
    private OutgoingMessageDispatcher dispatcher;
    private static readonly ActivitySource ActivitySource = new("PuzzleController");

    public MessageBusService(
        ILogger<MessageBusService> logger,
        ServiceBusClient client,
        MessageProcessor messageProcessor,
        ServiceBusHelper helper)
    {
        this.logger = logger;
        this.client = client;
        this.messageProcessor = messageProcessor;
        this.helper = helper;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("StartService");

        await helper.EnsureQueueExists("williampuzzlecontroller");
        await helper.EnsureQueueExists("williampuzzle");

        dispatcher = new OutgoingMessageDispatcher(helper, "williampuzzle");
        _ = Task.Run(() => dispatcher.RunAsync(), cancellationToken);

        processor = client.CreateProcessor("williampuzzlecontroller", new ServiceBusProcessorOptions
        {
            PrefetchCount = 10,
            MaxConcurrentCalls = 10,
        });

        processor.ProcessErrorAsync += ProcessError;
        processor.ProcessMessageAsync += messageProcessor.ProcessMessageAsync;

        activity?.SetTag("ProcessorCreated", true);
        activity?.SetTag("QueueName", "williampuzzlecontroller");

        await processor.StartProcessingAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine("Press 'R' to send a ResetPattern message");
            Console.WriteLine("Press a number to send a CaptureInput message with that number");
            Console.WriteLine("Press any other key to exit");

            await using var sender = client.CreateSender("williampuzzle");

            var key = Console.ReadKey();
            Console.WriteLine();

            if (key.Key == ConsoleKey.R)
            {
                var context = Propagators.DefaultTextMapPropagator.Extract(
                    new PropagationContext(Activity.Current?.Context ?? new ActivityContext(), Baggage.Current),
                    new Dictionary<string, string>(), (headers, key) => headers.TryGetValue(key, out var value) ? new[] { value } : Array.Empty<string>());

                Baggage.Current = context.Baggage;
                Activity.Current = null;

                using var activity = ActivitySource.StartActivity("SendResetPatternMessage",
                    ActivityKind.Server,
                    new ActivityContext(),
                    links: [new (context.ActivityContext)]);

                var resetMessage = new ServiceBusMessage(JsonSerializer.Serialize(new ResetPattern()));
                resetMessage.ApplicationProperties["MessageType"] = "ResetPattern";

                await sender.SendMessageAsync(resetMessage, stoppingToken);
                activity?.SetTag("MessageType", "ResetPattern");
                activity?.SetTag("MessageId", resetMessage.MessageId);
                Console.WriteLine("ResetPuzzle message sent\n");
            }
            else if (char.IsDigit(key.KeyChar))
            {
                using var activity = ActivitySource.StartActivity("SendCaptureInputMessage");
                var buttonNumber = int.Parse(key.KeyChar.ToString());
                var captureInputMessage = new ServiceBusMessage(JsonSerializer.Serialize(new CaptureInput { ButtonNumberPushed = buttonNumber }));
                captureInputMessage.ApplicationProperties["MessageType"] = "CaptureInput";

                await sender.SendMessageAsync(captureInputMessage, stoppingToken);

                activity?.SetTag("MessageType", "CaptureInput");
                activity?.SetTag("ButtonNumber", buttonNumber);
                activity?.SetTag("MessageId", captureInputMessage.MessageId);
                Console.WriteLine($"CaptureInput message with button number {buttonNumber} sent\n");
            }
            else
            {
                return;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("StopService");

        if (processor != null)
        {
            await processor.StopProcessingAsync(cancellationToken);
            await processor.DisposeAsync();
            activity?.SetTag("ProcessorStopped", true);
        }

        await base.StopAsync(cancellationToken);
    }

    private Task ProcessError(ProcessErrorEventArgs args)
    {
        using var activity = ActivitySource.StartActivity("ProcessError");
        activity?.SetTag("Error", args.Exception.Message);
        activity?.SetStatus(ActivityStatusCode.Error, args.Exception.Message);
        
        logger.LogError(args.Exception, "Error processing message");
        return Task.CompletedTask;
    }
}