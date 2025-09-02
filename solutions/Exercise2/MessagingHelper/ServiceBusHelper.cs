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
    }
}