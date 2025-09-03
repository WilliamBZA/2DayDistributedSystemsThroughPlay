using Azure.Messaging.ServiceBus;
using MessagingHelper;
using Microsoft.Data.SqlClient;

namespace PuzzleController.Services;

public class PuzzleMessageHandler : BackgroundService
{
    private readonly ServiceBusClient serviceBusClient;
    private ServiceBusProcessor? processor;
    private readonly ILogger<PuzzleMessageHandler> logger;

    public PuzzleMessageHandler(ILogger<PuzzleMessageHandler> logger, ServiceBusClient serviceBusClient)
    {
        this.serviceBusClient = serviceBusClient;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        processor = serviceBusClient.CreateProcessor("williampuzzlecontroller");
        
        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        finally
        {
            await processor.StopProcessingAsync(stoppingToken);
            await processor.DisposeAsync();
        }
    }

    static int retryCount = 0;

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        logger.LogInformation("Received message: {body}", body);

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
                    await args.CompleteMessageAsync(args.Message);
                }
            }
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Error processing message");
        return Task.CompletedTask;
    }
}