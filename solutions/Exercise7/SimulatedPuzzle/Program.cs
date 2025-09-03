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
            await helper.EnsureQueueExists("williampuzzlecontroller");
            await helper.EnsureQueueExists("williamdashboard");

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
                        await helper.SendMessageToQueue("williampuzzlecontroller", "{}");
                        await helper.SendMessageToQueue("williamdashboard", "{}");
                        Console.WriteLine("Message sent\n");
                        break;

                    default:
                        return;
                }
            }
        }
    }
}