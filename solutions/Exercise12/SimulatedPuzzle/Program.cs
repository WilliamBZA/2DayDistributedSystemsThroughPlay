using Azure.Messaging.ServiceBus;
using MessagingHelper;
using Puzzle.Messages;
using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace SimulatedPuzzle
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "connectionstring";
            await EnsureQueuesAndTopicsExist(connectionString);

            var client = new ServiceBusClient(connectionString);
            var processor = client.CreateProcessor("williampuzzle", new ServiceBusProcessorOptions
            {
            });

            processor.ProcessErrorAsync += Processor_ProcessErrorAsync;
            processor.ProcessMessageAsync += args =>
            {
                var messageType = args.Message.ApplicationProperties["MessageType"].ToString();
                var body = args.Message.Body;

                switch (messageType)
                {
                    case "ShowSequence":
                        var showSequence = JsonSerializer.Deserialize<ShowSequence>(body);
                        Console.WriteLine($"Show Puzzle Sequence");
                        break;

                    case "ShowSolved":
                        var showSolved = JsonSerializer.Deserialize<ShowSolved>(body);
                        Console.WriteLine($"Show solved");
                        break;

                    case "ShowFailed":
                        var showFailed = JsonSerializer.Deserialize<ShowFailed>(body);
                        Console.WriteLine($"Show failed");
                        break;

                    case "ResetPattern":
                        var resetPattern = JsonSerializer.Deserialize<ResetPattern>(body);
                        Console.WriteLine($"Reset Pattern");
                        break;

                    case "CaptureInput":
                        var captureInput = JsonSerializer.Deserialize<CaptureInput>(body);
                        Console.WriteLine($"Input captured - button number {captureInput.ButtonNumberPushed} pushed");
                        break;

                    default:
                        Console.WriteLine("Unknown message type.");
                        break;
                }

                return Task.CompletedTask;
            };

            await processor.StartProcessingAsync();

            while (true)
            {
                Console.WriteLine("Press 'P' to send a message to the Puzzle Controller");
                Console.WriteLine("Press 'S' to send 100 messages");
                Console.WriteLine("Press any other key to exit");

                // sending here instead of in the helper because we're using batching
                await using var sender = client.CreateSender("puzzleevents");

                var key = Console.ReadKey();
                Console.WriteLine();
                switch (key.Key)
                {
                    case ConsoleKey.S:
                        Console.WriteLine("Sending...");

                        var batch = await sender.CreateMessageBatchAsync();
                        for (var i = 0; i < 100; i++)
                        {
                            var message = new ServiceBusMessage($"Message {i}");
                            if (!batch.TryAddMessage(message))
                            {
                                await sender.SendMessagesAsync(batch);
                                batch = await sender.CreateMessageBatchAsync();
                                batch.TryAddMessage(message);
                            }
                        }
                        await sender.SendMessagesAsync(batch);

                        Console.WriteLine("Message sent\n");
                        break;

                    case ConsoleKey.P:
                        var puzzleMessage = new ServiceBusMessage("{}");
                        puzzleMessage.ApplicationProperties["MessageType"] = "PuzzleEvent";

                        await sender.SendMessageAsync(puzzleMessage);
                        Console.WriteLine("Message sent\n");
                        break;

                    default:
                        return;
                }
            }
        }

        private static Task Processor_ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            Console.WriteLine("Error");
            return Task.CompletedTask;
        }

        private static async Task EnsureQueuesAndTopicsExist(string connectionString)
        {
            var helper = new ServiceBusHelper(connectionString);

            // Make sure the queues exist
            await helper.EnsureQueueExists("williampuzzlecontroller");
            await helper.EnsureQueueExists("williamdashboard");
            await helper.EnsureQueueExists("williampuzzle");

            // Make sure the topic exists
            await helper.EnsureTopicExists("puzzleevents");

            // Make sure the subscriptions exist
            await helper.EnsureSubscriptionExists(topicName: "puzzleevents",
               subscriptionName: "williampuzzlecontroller-subscription",
               queueToForwardTo: "williampuzzlecontroller");

            await helper.EnsureSubscriptionExists(topicName: "puzzleevents",
               subscriptionName: "williamdashboard-subscription",
               queueToForwardTo: "williamdashboard");
        }
    }
}