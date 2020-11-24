using System;
using System.Diagnostics;
using System.Net.Http;
using ChildWorkerService.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using NServiceBus.Extensions.Diagnostics.OpenTelemetry;
using NServiceBus.Json;
using OpenTelemetry.Trace;

namespace WorkerService
{
    public class Program
    {
        public const string EndpointName = "NsbActivities.WorkerService";

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

                    var routing = transport.Routing();
                    routing.RouteToEndpoint(typeof(MakeItYell).Assembly, "NsbActivities.ChildWorkerService");

                    endpointConfiguration.UsePersistence<LearningPersistence>();

                    endpointConfiguration.EnableInstallers();

                    endpointConfiguration.AuditProcessedMessagesTo("NsbActivities.Audit");

                    endpointConfiguration.AuditSagaStateChanges("Particular.ServiceControl.2");

                    endpointConfiguration.PurgeOnStartup(true);

                    // configure endpoint here
                    return endpointConfiguration;
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddOpenTelemetry(builder => builder
                        .UseZipkinExporter(o =>
                        {
                            o.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
                            o.ServiceName = EndpointName;
                        })
                        .UseJaegerExporter(c =>
                        {
                            c.AgentHost = "localhost";
                            c.AgentPort = 6831;
                            c.ServiceName = EndpointName;
                        })
                        .AddNServiceBusInstrumentation(opt => opt.CaptureMessageBody = true)
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation());

                    services.AddScoped<Func<HttpClient>>(s => () => new HttpClient
                    {
                        BaseAddress = new Uri("https://localhost:5001")
                    });
                })
        ;
    }
}
