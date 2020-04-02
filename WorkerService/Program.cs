using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using NServiceBus.Diagnostics.OpenTelemetry;
using NServiceBus.Json;
using OpenTelemetry.Trace.Configuration;

namespace WorkerService
{
    public class Program
    {
        private const string EndpointName = "NsbActivities.WorkerService";

        public static void Main(string[] args)
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseMicrosoftLogFactoryLogging()
                .UseNServiceBus(hostBuilderContext =>
                {
                    var endpointConfiguration = new EndpointConfiguration(EndpointName);

                    endpointConfiguration.UseSerialization<SystemJsonSerializer>();

                    var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
                    transport.ConnectionString("host=localhost");
                    transport.UseConventionalRoutingTopology();

                    endpointConfiguration.UsePersistence<LearningPersistence>();

                    endpointConfiguration.EnableInstallers();

                    endpointConfiguration.AuditProcessedMessagesTo("NsbActivities.Audit");

                    endpointConfiguration.PurgeOnStartup(true);

                    // configure endpoint here
                    return endpointConfiguration;
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddOpenTelemetry(builder =>
                    {
                        builder
                            .UseZipkin(o =>
                            {
                                o.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
                                o.ServiceName = EndpointName;
                            })
                            .UseApplicationInsights(c =>
                            {
                                c.InstrumentationKey = context.Configuration.GetValue<string>("ApplicationInsights:InstrumentationKey");
                            })
                            .AddNServiceBusCollector()
                            .AddRequestCollector()
                            .AddDependencyCollector();
                    });
                })
        ;
    }
}
