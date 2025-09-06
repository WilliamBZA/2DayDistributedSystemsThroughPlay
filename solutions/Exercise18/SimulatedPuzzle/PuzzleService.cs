using Azure.Messaging.ServiceBus;
using MessagingHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Puzzle.Messages;
using System.Text.Json;
using System.Diagnostics;

namespace SimulatedPuzzle;

public class PuzzleService : IHostedService
{
    private readonly ServiceBusClient client;
    private ServiceBusProcessor? processor;
    private ServiceBusSender? sender;
    private readonly string connectionString;
    private readonly ILogger<PuzzleService> logger;
    private readonly string staticId;
    private static readonly ActivitySource ActivitySource = new("SimulatedPuzzle");

    public PuzzleService(ILogger<PuzzleService> logger)
    {
        connectionString = "connectionstring";
        client = new ServiceBusClient(connectionString);
        staticId = Guid.NewGuid().ToString();

        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("StartService");
        
        await EnsureQueuesAndTopicsExist();

        processor = client.CreateProcessor("williampuzzle", new ServiceBusProcessorOptions());
        sender = client.CreateSender("puzzleevents");

        processor.ProcessErrorAsync += ProcessorErrorHandler;
        processor.ProcessMessageAsync += ProcessMessageHandler;

        await processor.StartProcessingAsync(cancellationToken);

        _ = RunUserInterface(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (processor != null)
        {
            await processor.StopProcessingAsync(cancellationToken);
            await processor.DisposeAsync();
        }

        if (sender != null)
        {
            await sender.DisposeAsync();
        }

        await client.DisposeAsync();
    }

    private async Task RunUserInterface(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Press 'P' to send a message to the Puzzle Controller");
            Console.WriteLine($"Press 'I' to send a message to the Puzzle Controller with ID {staticId}");
            Console.WriteLine("Press 'S' to send 100 messages");
            Console.WriteLine("Press any other key to exit");

            var key = Console.ReadKey();
            Console.WriteLine();

            if (sender == null) continue;

            switch (key.Key)
            {
                case ConsoleKey.S:
                    await SendBatchMessages(cancellationToken);
                    break;

                case ConsoleKey.P:
                    await SendPuzzleMessage(false, cancellationToken);
                    break;

                case ConsoleKey.I:
                    await SendPuzzleMessage(true, cancellationToken);
                    break;

                default:
                    Environment.Exit(0);
                    return;
            }
        }
    }

    private async Task SendBatchMessages(CancellationToken cancellationToken)
    {
        if (sender == null) return;

        using var activity = ActivitySource.StartActivity("SendBatchMessages");
        activity?.SetTag("BatchSize", 100);

        logger.LogInformation("Sending batch messages...");
        var batch = await sender.CreateMessageBatchAsync(cancellationToken);
        
        for (var i = 0; i < 100; i++)
        {
            var message = new ServiceBusMessage($"Message {i}");
            if (!batch.TryAddMessage(message))
            {
                await sender.SendMessagesAsync(batch, cancellationToken);
                batch = await sender.CreateMessageBatchAsync(cancellationToken);
                batch.TryAddMessage(message);
            }
        }
        
        await sender.SendMessagesAsync(batch, cancellationToken);
        logger.LogInformation("Batch messages sent\n");
    }

    private async Task SendPuzzleMessage(bool withId, CancellationToken cancellationToken)
    {
        if (sender == null) return;

        using var activity = ActivitySource.StartActivity("SendPuzzleMessage");
        activity?.SetTag("WithId", withId);
        if (withId)
        {
            activity?.SetTag("MessageId", staticId);
        }

        var puzzleMessage = new ServiceBusMessage("{}");
        puzzleMessage.ApplicationProperties["MessageType"] = "PuzzleEvent";
        
        if (withId)
        {
            puzzleMessage.MessageId = staticId;
        }

        await sender.SendMessageAsync(puzzleMessage, cancellationToken);
        logger.LogInformation("Message sent\n");
    }

    private Task ProcessMessageHandler(ProcessMessageEventArgs args)
    {
        using var activity = ActivitySource.StartActivity("ProcessMessage", ActivityKind.Server, new ActivityContext(), links: []);
        var messageType = args.Message.ApplicationProperties["MessageType"].ToString();
        var body = args.Message.Body;

        activity?.SetTag("MessageType", messageType);
        activity?.SetTag("MessageId", args.Message.MessageId);

        switch (messageType)
        {
            case "ShowSequence":
                var showSequence = JsonSerializer.Deserialize<ShowSequence>(body);
                logger.LogInformation("Show Puzzle Sequence");
                break;

            case "ShowSolved":
                var showSolved = JsonSerializer.Deserialize<ShowSolved>(body);
                logger.LogInformation("Show solved");
                break;

            case "ShowFailed":
                var showFailed = JsonSerializer.Deserialize<ShowFailed>(body);
                logger.LogInformation("Show failed");
                break;

            case "ResetPattern":
                var resetPattern = JsonSerializer.Deserialize<ResetPattern>(body);
                logger.LogInformation("Reset Pattern");
                break;

            case "CaptureInput":
                var captureInput = JsonSerializer.Deserialize<CaptureInput>(body);
                logger.LogInformation("Input captured - button number {ButtonNumber} pushed", captureInput.ButtonNumberPushed);
                activity?.SetTag("ButtonNumber", captureInput.ButtonNumberPushed);
                break;

            default:
                logger.LogWarning("Unknown message type: {MessageType}", messageType);
                activity?.SetTag("Error", "UnknownMessageType");
                break;
        }

        return Task.CompletedTask;
    }

    private Task ProcessorErrorHandler(ProcessErrorEventArgs arg)
    {
        using var activity = ActivitySource.StartActivity("ProcessError");
        activity?.SetTag("Error", arg.Exception.Message);
        activity?.SetStatus(ActivityStatusCode.Error, arg.Exception.Message);
        
        logger.LogError(arg.Exception, "Error processing message");
        return Task.CompletedTask;
    }

    private async Task EnsureQueuesAndTopicsExist()
    {
        using var activity = ActivitySource.StartActivity("EnsureQueuesAndTopicsExist");
        
        var helper = new ServiceBusHelper(connectionString);

        // Make sure the queues exist
        await helper.EnsureQueueExists("williampuzzlecontroller");
        await helper.EnsureQueueExists("williamdashboard");
        await helper.EnsureQueueExists("williampuzzle");

        // Make sure the topic exists
        await helper.EnsureTopicExists("puzzleevents");

        // Make sure the subscriptions exist
        await helper.EnsureSubscriptionExists(
            topicName: "puzzleevents",
            subscriptionName: "williampuzzlecontroller-subscription",
            queueToForwardTo: "williampuzzlecontroller");

        await helper.EnsureSubscriptionExists(
            topicName: "puzzleevents",
            subscriptionName: "williamdashboard-subscription",
            queueToForwardTo: "williamdashboard");
            
        activity?.SetTag("QueuesCreated", 3);
        activity?.SetTag("TopicsCreated", 1);
        activity?.SetTag("SubscriptionsCreated", 2);
    }
}