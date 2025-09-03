using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace MessagingHelper
{
    public class ServiceBusHelper(string connectionString)
    {
        public async Task EnsureQueueExists(string queueName)
        {
            var admin = new ServiceBusAdministrationClient(connectionString);

            if (!await admin.QueueExistsAsync(queueName))
            {
                var createOptions = new CreateQueueOptions(queueName)
                {
                };

                await admin.CreateQueueAsync(createOptions);
            }
        }

        public async Task SendMessageToQueue(string queueName, string messageBody)
        {
            await using var client = new ServiceBusClient(connectionString);
            await using var sender = client.CreateSender(queueName);

            var message = new ServiceBusMessage(messageBody);
            await sender.SendMessageAsync(message);
        }
    }
}