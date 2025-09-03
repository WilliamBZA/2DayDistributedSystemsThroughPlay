using Azure.Messaging.ServiceBus;
using MessagingHelper;
using System.Runtime.ExceptionServices;

namespace SimulatedPuzzle
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "connectionstring";

            var helper = new ServiceBusHelper(connectionString);

            // Make sure the queues exist
            await helper.EnsureQueueExists("williampuzzlecontroller");
            await helper.EnsureQueueExists("williamdashboard");

            // Make sure the topic exists
            await helper.EnsureTopicExists("puzzleevents");

            // Make sure the subscriptions exist
            await helper.EnsureSubscriptionExists(topicName: "puzzleevents",
               subscriptionName: "williampuzzlecontroller-subscription",
               queueToForwardTo: "williampuzzlecontroller");

            await helper.EnsureSubscriptionExists(topicName: "puzzleevents",
               subscriptionName: "williamdashboard-subscription",
               queueToForwardTo: "williamdashboard");

            while (true)
            {
                Console.WriteLine("Press 'S' to send 100 messages");
                Console.WriteLine("Press any other key to exit");

                // sending here instead of in the helper because we're using batching
                await using var client = new ServiceBusClient(connectionString);
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

                    default:
                        return;
                }
            }
        }
    }
}