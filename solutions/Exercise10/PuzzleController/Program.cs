using Azure.Messaging.ServiceBus;
using MessagingHelper;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace PuzzleController
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "connectionstring";

            var helper = new ServiceBusHelper(connectionString);

            await helper.EnsureQueueExists("williampuzzlecontroller");

            var client = new ServiceBusClient(connectionString);
            var processor = client.CreateProcessor("williampuzzlecontroller", new ServiceBusProcessorOptions
            {
                PrefetchCount = 10,
                MaxConcurrentCalls = 10,
            });

            processor.ProcessErrorAsync += Processor_ProcessErrorAsync;

            var stopWatch = new Stopwatch();
            var numberOfMessagesProcessed = 0;

            var retryCount = 0;
            processor.ProcessMessageAsync += async e =>
            {
                var currentCount = Interlocked.Increment(ref numberOfMessagesProcessed);

                // Upsert customer with $20 added to total
                using (var connection = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=CopenhagenWorkshop;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False"))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = @"
                    MERGE Customers AS target
                    USING (SELECT @CustomerName AS CustomerName) AS source
                    ON target.CustomerName = source.CustomerName
                    WHEN MATCHED THEN
                        UPDATE SET Total = Total + 20
                    WHEN NOT MATCHED THEN
                        INSERT (ID, CustomerName, Total)
                        VALUES (NEWID(), @CustomerName, 20);";

                            command.Parameters.Add("CustomerName", System.Data.SqlDbType.NVarChar).Value = "NDC Workshops";
                            await command.ExecuteNonQueryAsync();

                            if (retryCount++ < 3)
                            {
                                throw new Exception("Ooops!");
                            }

                            transaction.Commit();
                            connection.Close();
                        }
                    }
                }

                await MakeRequest("https://www.google.com/search?client=firefox-b-d&q=how+to+waste+time+doing+stuff+that+&sei=rma4aLH0EYzRhbIPubDN6Qo");

                Console.WriteLine(currentCount);

                if (currentCount >= 100)
                {
                    stopWatch.Stop();
                    Console.WriteLine($"It took {stopWatch.ElapsedMilliseconds}ms to process 100 messages...");
                }
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

        private static readonly HttpClient httpClient = new HttpClient();
        static async Task MakeRequest(string url)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
