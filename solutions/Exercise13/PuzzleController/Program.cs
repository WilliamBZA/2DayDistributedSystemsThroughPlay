using Azure.Messaging.ServiceBus;
using MessagingHelper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Puzzle.Messages;
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

            processor.ProcessMessageAsync += async args =>
            {
                var body = args.Message.Body.ToString();
                Console.WriteLine($"Received message: {body}");

                if (args.Message.DeliveryCount > 1)
                {
                    Console.WriteLine($"Delivery count: {args.Message.DeliveryCount} for message {args.Message.MessageId}. Delaying by 10 seconds.");
                    await Task.Delay(10 * 1000);
                }

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

                            if (args.Message.EnqueuedTime.AddMinutes(1) > DateTime.UtcNow)
                            {
                                throw new Exception("Ooops!");
                            }

                            Console.WriteLine($"Processed message {args.Message.MessageId}");
                            transaction.Commit();
                            connection.Close();
                            await args.CompleteMessageAsync(args.Message);
                        }
                    }
                }
            };

            await processor.StartProcessingAsync();

            while (true)
            {
                Console.WriteLine("Press 'R' to send a ResetPattern message");
                Console.WriteLine("Press a number to send a CaptureInput message with that number");
                Console.WriteLine("Press any other key to exit");

                await using var sender = client.CreateSender("williampuzzle");

                var key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key == ConsoleKey.R)
                {
                    var resetMessage = new ServiceBusMessage(JsonSerializer.Serialize(new ResetPattern()));
                    resetMessage.ApplicationProperties["MessageType"] = "ResetPattern";

                    await sender.SendMessageAsync(resetMessage);
                    Console.WriteLine("ResetPuzzle message sent\n");
                }
                else if (char.IsDigit(key.KeyChar))
                {
                    var buttonNumber = int.Parse(key.KeyChar.ToString());
                    var captureInputMessage = new ServiceBusMessage(JsonSerializer.Serialize(new CaptureInput { ButtonNumberPushed = buttonNumber }));
                    captureInputMessage.ApplicationProperties["MessageType"] = "CaptureInput";

                    await sender.SendMessageAsync(captureInputMessage);

                    Console.WriteLine($"CaptureInput message with button number {buttonNumber} sent\n");
                }
                else
                {
                    return;
                }
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
