using System;
using System.Diagnostics;
using System.Net.Http;
using ChildWorkerService.Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using ObservabilityExtensions;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WorkerService;

public class Program
{
    public const string EndpointName = "NsbActivities.WorkerService";

    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
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

                endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

                var transport = new RabbitMQTransport(
                    RoutingTopology.Conventional(QueueType.Classic),
                    "host=localhost"
                );
                var transportSettings = endpointConfiguration.UseTransport(transport);

                transportSettings.RouteToEndpoint(typeof(MakeItYell).Assembly, "NsbActivities.ChildWorkerService");

                endpointConfiguration.UsePersistence<LearningPersistence>();

                endpointConfiguration.EnableInstallers();

                endpointConfiguration.EnableOpenTelemetry();
                // configure endpoint here
                return endpointConfiguration;
            })
            .ConfigureWebHostDefaults(webHostBuilder =>
            {
                webHostBuilder.ConfigureServices((context, services) =>
                {
                    services.AddScoped<Func<HttpClient>>(_ => () => new HttpClient
                    {
                        BaseAddress = new Uri("https://localhost:5001")
                    });

                    var honeycombOptions = context.Configuration.GetHoneycombOptions();

                    services.AddOpenTelemetry()
                        .WithTracing(builder =>
                        {
                            builder
                                .ConfigureResource(resource => resource.AddService(Program.EndpointName))
                                .AddSource("NServiceBus.Core")
                                .AddHttpClientInstrumentation()
                                .AddProcessor(new CopyBaggageToTagsProcessor())
                                .AddHoneycomb(honeycombOptions)
                                .AddZipkinExporter(o =>
                                {
                                    o.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
                                })
                                .AddJaegerExporter(c =>
                                {
                                    c.AgentHost = "localhost";
                                    c.AgentPort = 6831;
                                });
                        })
                        .WithMetrics(builder =>
                        {
                            builder
                                .ConfigureResource(resource => resource.AddService(Program.EndpointName))
                                .AddMeter("NServiceBus.Core")
                                .AddPrometheusExporter(options =>
                                {
                                    options.ScrapeResponseCacheDurationMilliseconds = 0;
                                });
                        })
                        ;
                });
                webHostBuilder.Configure(app =>
                {
                    app.UseOpenTelemetryPrometheusScrapingEndpoint();
                });
            })
    ;
}