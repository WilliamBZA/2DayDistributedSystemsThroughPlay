using Azure.Messaging.ServiceBus;
using MessagingHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PuzzleController.Services;

namespace PuzzleController;

public class Program
{
    public static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var serviceBusConnectionString = "connectionstring";
                var sqlConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=CopenhagenWorkshop;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";

                services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
                services.AddSingleton(new ServiceBusHelper(serviceBusConnectionString));
                services.AddSingleton(sp => new MessageProcessor(
                    sp.GetRequiredService<ILogger<MessageProcessor>>(),
                    sqlConnectionString,
                    serviceBusConnectionString));

                services.AddOpenTelemetry()
                    .WithTracing(builder => builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService("PuzzleController"))
                        .AddSource("PuzzleController")
                        .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
                        .AddAspNetCoreInstrumentation()
                    );

                services.AddHostedService<MessageBusService>();
            })
            .RunConsoleAsync();
    }
}
