using Azure.Messaging.ServiceBus;

namespace SimulatedPuzzle
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "Connectionstring";

            await using var client = new ServiceBusClient(connectionString);
            await using var receiver = client.CreateReceiver("williampuzzle");
            
            var message = await receiver.ReceiveMessageAsync();

            var body = System.Text.Encoding.UTF8.GetString(message.Body.ToArray());

            Console.WriteLine($"Message reeceived: {body}");

            await receiver.CompleteMessageAsync(message);
        }
    }
}