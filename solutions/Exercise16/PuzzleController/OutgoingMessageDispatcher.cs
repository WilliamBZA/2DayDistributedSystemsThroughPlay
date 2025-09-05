using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MessagingHelper;
using System.Text.Json;

namespace PuzzleController
{
    public class OutgoingMessageDispatcher
    {
        private readonly ServiceBusHelper serviceBusHelper;
        private readonly string queueName;

        public OutgoingMessageDispatcher(ServiceBusHelper serviceBusHelper, string queueName)
        {
            this.serviceBusHelper = serviceBusHelper;
            this.queueName = queueName;
        }

        public async Task RunAsync()
        {
            while (true)
            {
                try
                {
                    using (var connection = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=CopenhagenWorkshop;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False;MultipleActiveResultSets=True"))
                    {
                        await connection.OpenAsync();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"SELECT Id, Body, Headers FROM OutgoingMessages WHERE SentAt IS NULL";
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var messageId = reader.GetInt32(0);
                                    var messageBody = reader.GetString(1);
                                    
                                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(2));

                                    var transportMessageId = await serviceBusHelper.SendMessageToQueue(queueName, messageBody, headers);

                                    // Update SentAt
                                    using (var updateCommand = connection.CreateCommand())
                                    {
                                        updateCommand.CommandText = @"UPDATE OutgoingMessages SET
SentAt = @SentAt,
TransportMessageId = @TransportMessageId
WHERE Id = @Id";
                                        updateCommand.Parameters.Add("@SentAt", SqlDbType.DateTime).Value = DateTime.UtcNow;
                                        updateCommand.Parameters.Add("@Id", SqlDbType.Int).Value = messageId;
                                        updateCommand.Parameters.Add("@TransportMessageId", SqlDbType.NVarChar).Value = transportMessageId;

                                        await updateCommand.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OutgoingMessageDispatcher error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}
