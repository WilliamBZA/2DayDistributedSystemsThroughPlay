using Azure.Messaging.ServiceBus;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Puzzle.Messages;
using PuzzleController.Messages;
using System.Data;
using System.Text.Json;

namespace PuzzleController.Services;

public class MessageProcessor
{
    private readonly ILogger<MessageProcessor> _logger;
    private readonly string _connectionString;
    private readonly string _serviceBusConnectionString;

    public MessageProcessor(ILogger<MessageProcessor> logger, string connectionString, string serviceBusConnectionString)
    {
        _logger = logger;
        _connectionString = connectionString;
        _serviceBusConnectionString = serviceBusConnectionString;
    }

    public async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        var body = args.Message.Body.ToString();
        _logger.LogInformation("Received message: {Body}", body);

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        
        if (!await TryDeduplicateMessage(connection, transaction, messageId))
        {
            await args.CompleteMessageAsync(args.Message);
            return;
        }

        // Upsert customer with $20 added to total
        await UpsertCustomer(connection, transaction);
        
        await PublishMessage(new CustomerTotalIncreased
        {
            IncreaseAmount = 20,
            TotalAfterIncrease = 20
        }, connection, transaction);

        _logger.LogInformation("Processed message {MessageId}", args.Message.MessageId);
        transaction.Commit();
        await args.CompleteMessageAsync(args.Message);
    }

    private async Task<bool> TryDeduplicateMessage(SqlConnection connection, SqlTransaction transaction, string messageId)
    {
        using var deduplicationCommand = connection.CreateCommand();
        deduplicationCommand.Transaction = transaction;
        deduplicationCommand.CommandText = @"
            INSERT INTO PuzzleControllerMessageDeduplication (MessageId, ProcessedAt)
            VALUES (@MessageId, GETDATE())";
        deduplicationCommand.Parameters.Add("@MessageId", SqlDbType.NVarChar).Value = messageId;

        try
        {
            await deduplicationCommand.ExecuteNonQueryAsync();
            return true;
        }
        catch (SqlException ex) when (ex.Number == 2627)
        {
            _logger.LogInformation("Duplicate message. Discarding.");
            return false;
        }
    }

    private async Task UpsertCustomer(SqlConnection connection, SqlTransaction transaction)
    {
        using var command = connection.CreateCommand();
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

        command.Parameters.Add("CustomerName", SqlDbType.NVarChar).Value = "NDC Workshops";
        await command.ExecuteNonQueryAsync();
    }

    private async Task PublishMessage(CustomerTotalIncreased customerTotalIncreased, SqlConnection connection, SqlTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            INSERT INTO OutgoingMessages (Body, Headers, DestinationAddress)
            VALUES (@MessageBody, @MessageHeaders, @DestinationAddress)";

        command.Parameters.Add("@MessageBody", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(customerTotalIncreased);
        command.Parameters.Add("@MessageHeaders", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(new Dictionary<string, string> { { "MessageType", "CustomerTotalIncreased" } });
        command.Parameters.Add("@DestinationAddress", SqlDbType.NVarChar).Value = "williampuzzle";

        await command.ExecuteNonQueryAsync();
    }
}