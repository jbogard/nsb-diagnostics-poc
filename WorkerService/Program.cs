using System;
using System.Diagnostics;
using System.Net.Http;
using ChildWorkerService.Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Extensions.Diagnostics;
using NServiceBus.Json;
using NServiceBus.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WorkerService
{
    public class Program
    {
        public const string EndpointName = "NsbActivities.WorkerService";

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

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
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

                    var settings = endpointConfiguration.GetSettings();

                    settings.Set(new NServiceBus.Extensions.Diagnostics.InstrumentationOptions
                    {
                        CaptureMessageBody = true
                    });

                    endpointConfiguration.EnableFeature<DiagnosticsMetricsFeature>();

                    // configure endpoint here
                    return endpointConfiguration;
                })
                .ConfigureWebHostDefaults(webHostBuilder =>
                {
                    webHostBuilder.ConfigureServices(services =>
                    {
                        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(EndpointName);
                        services.AddOpenTelemetryTracing(builder =>
                        {
                            builder
                                .SetResourceBuilder(resourceBuilder)
                                .AddNServiceBusInstrumentation()
                                .AddHttpClientInstrumentation()
                                .AddZipkinExporter(o => { o.Endpoint = new Uri("http://localhost:9411/api/v2/spans"); })
                                .AddJaegerExporter(c =>
                                {
                                    c.AgentHost = "localhost";
                                    c.AgentPort = 6831;
                                });
                        });

                        services.AddOpenTelemetryMetrics(builder =>
                        {
                            builder.SetResourceBuilder(resourceBuilder)
                                .AddHttpClientInstrumentation()
                                .AddNServiceBusInstrumentation()
                                .AddPrometheusExporter(options =>
                                {
                                    options.ScrapeResponseCacheDurationMilliseconds = 0;
                                });
                        });

                        services.AddScoped<Func<HttpClient>>(s => () => new HttpClient
                        {
                            BaseAddress = new Uri("https://localhost:5001")
                        });
                    });

                    webHostBuilder.Configure(app =>
                    {
                        app.UseOpenTelemetryPrometheusScrapingEndpoint();
                    });
                })
        ;
    }
}
