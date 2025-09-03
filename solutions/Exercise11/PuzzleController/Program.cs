using Azure.Messaging.ServiceBus;
using MessagingHelper;
using Microsoft.Data.SqlClient;
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

            var stopWatch = new Stopwatch();
            var numberOfMessagesProcessed = 0;

            processor.ProcessMessageAsync += args =>
            {
                var currentCount = Interlocked.Increment(ref numberOfMessagesProcessed);
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
