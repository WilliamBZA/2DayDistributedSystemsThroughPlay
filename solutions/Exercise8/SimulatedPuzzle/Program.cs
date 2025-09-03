using Azure.Messaging.ServiceBus;
using MessagingHelper;

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
                Console.WriteLine("Press 'C' to send a PuzzleCompleted message");
                Console.WriteLine("Press any other key to exit");

                var key = Console.ReadKey();
                Console.WriteLine();
                switch (key.Key)
                {
                    case ConsoleKey.C:
                        Console.WriteLine("Sending...");
                        await helper.SendMessageToTopic("puzzleevents", "{}");
                        Console.WriteLine("Message sent\n");
                        break;

                    default:
                        return;
                }
            }
        }
    }
}