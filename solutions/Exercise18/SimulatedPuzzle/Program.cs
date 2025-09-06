using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SimulatedPuzzle;

internal class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<PuzzleService>();
                
                services.AddOpenTelemetry()
                    .WithTracing(builder => builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService("SimulatedPuzzle"))
                        .AddSource("SimulatedPuzzle")
                        .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
                        .AddAspNetCoreInstrumentation()
                    );
            })
            .Build();

        await host.RunAsync();
    }
}