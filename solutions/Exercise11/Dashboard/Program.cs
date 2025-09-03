using Azure.Messaging.ServiceBus;
using MessagingHelper;
using System.Diagnostics;

namespace Dashboard
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "connectionstring";

            var helper = new ServiceBusHelper(connectionString);

            await helper.EnsureQueueExists("williamdashboard");

            var client = new ServiceBusClient(connectionString);
            var processor = client.CreateProcessor("williamdashboard", new ServiceBusProcessorOptions
            {
                PrefetchCount = 10,
                MaxConcurrentCalls = 10,
            });

            processor.ProcessErrorAsync += Processor_ProcessErrorAsync;

            var stopWatch = new Stopwatch();
            var numberOfMessagesProcessed = 0;
            processor.ProcessMessageAsync += e =>
            {
                var currentCount = Interlocked.Increment(ref numberOfMessagesProcessed);

                for (int i = 0; i < 100000; i++)
                {
                    // Simulate work
                    var ii = Math.Cos(i * i) * 999999;
                    if (ii % 2 == 0)
                    {
                        ii--;
                    }
                    else
                    {
                        ii = (int)Math.Sqrt(ii - 1);
                    }
                }

                Console.WriteLine(currentCount);

                if (currentCount >= 100)
                {
                    stopWatch.Stop();
                    Console.WriteLine($"It took {stopWatch.ElapsedMilliseconds}ms to process 100 messages...");
                }

                return Task.CompletedTask;
            };

            await processor.StartProcessingAsync();
            stopWatch.Start();

            while (true)
            {
                Console.WriteLine("Press any key to exit");

                var key = Console.ReadKey();
                Console.WriteLine();

                return;
            }
        }

        private static Task Processor_ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            Console.WriteLine("Error");
            return Task.CompletedTask;
        }
    }
}