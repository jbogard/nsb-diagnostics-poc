using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using WorkerService.Messages;

namespace WebApplication;

public class Program
{
    public const string EndpointName = "NsbActivities.WebApplication";

    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        SeedDb(host);

        host.Run();
    }

    private static void SeedDb(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<WeatherContext>();
            DbInitializer.Initialize(context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred creating the DB.");
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.AddOpenTelemetry(options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                    options.ParseStateValues = true;
                    options.AddConsoleExporter();
                    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(EndpointName));
                });
            })
            .UseNServiceBus(_ =>
            {
                var endpointConfiguration = new EndpointConfiguration(EndpointName);

                var transport = new RabbitMQTransport(
                    RoutingTopology.Conventional(QueueType.Classic),
                    "host=localhost"
                );
                var transportSettings = endpointConfiguration.UseTransport(transport);

                transportSettings.RouteToEndpoint(typeof(SaySomething).Assembly, "NsbActivities.WorkerService");

                endpointConfiguration.UsePersistence<LearningPersistence>();
                endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

                endpointConfiguration.EnableInstallers();

                endpointConfiguration.EnableOpenTelemetry();
                // configure endpoint here
                return endpointConfiguration;
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
    ;
}