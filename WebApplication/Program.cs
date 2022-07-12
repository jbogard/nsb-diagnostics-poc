using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Extensions.Diagnostics;
using NServiceBus.Json;
using WorkerService.Messages;

namespace WebApplication
{
    public class Program
    {
        public const string EndpointName = "NsbActivities.WebApplication";

        public static void Main(string[] args)
        {
            var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                ActivityStopped = activity =>
                {
                    foreach (var (key, value) in activity.Baggage)
                    {
                        activity.AddTag(key, value);
                    }
                }
            };
            ActivitySource.AddActivityListener(listener);










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
                .UseNServiceBus(_ =>
                {
                    var endpointConfiguration = new EndpointConfiguration(EndpointName);

                    var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
                    transport.ConnectionString("host=localhost");
                    transport.UseConventionalRoutingTopology();

                    var routing = transport.Routing();
                    routing.RouteToEndpoint(typeof(SaySomething).Assembly, "NsbActivities.WorkerService");

                    endpointConfiguration.UsePersistence<LearningPersistence>();
                    endpointConfiguration.UseSerialization<SystemJsonSerializer>();

                    endpointConfiguration.EnableInstallers();

                    endpointConfiguration.AuditProcessedMessagesTo("NsbActivities.Audit");

                    endpointConfiguration.EnableFeature<DiagnosticsMetricsFeature>();

                    var settings = endpointConfiguration.GetSettings();

                    settings.Set(new InstrumentationOptions
                    {
                        CaptureMessageBody = true
                    });

                    // configure endpoint here
                    return endpointConfiguration;
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                ;
    }
}
