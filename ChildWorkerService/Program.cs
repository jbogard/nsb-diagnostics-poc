using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mongo2Go;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using NServiceBus;
using ObservabilityExtensions;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using InstrumentationOptions = MongoDB.Driver.Core.Extensions.DiagnosticSources.InstrumentationOptions;

namespace ChildWorkerService;

public class Program
{
    public const string EndpointName = "NsbActivities.ChildWorkerService";

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
                endpointConfiguration.UseTransport(transport);

                endpointConfiguration.UsePersistence<LearningPersistence>();

                endpointConfiguration.EnableInstallers();

                var recoverability = endpointConfiguration.Recoverability();
                recoverability.Immediate(i => i.NumberOfRetries(1));
                recoverability.Delayed(i => i.NumberOfRetries(0));

                endpointConfiguration.EnableOpenTelemetry();
                // configure endpoint here
                return endpointConfiguration;
            })
            .ConfigureWebHostDefaults(webHostBuilder =>
            {

                webHostBuilder.ConfigureServices((context, services) =>
                {
                    var runner = MongoDbRunner.Start(singleNodeReplSet: true, singleNodeReplSetWaitTimeout: 20);

                    services.AddSingleton(runner);
                    var urlBuilder = new MongoUrlBuilder(runner.ConnectionString)
                    {
                        DatabaseName = "dev"
                    };
                    var mongoUrl = urlBuilder.ToMongoUrl();
                    var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);

                    mongoClientSettings.ClusterConfigurator = cb =>
                        cb.Subscribe(new DiagnosticsActivityEventSubscriber(
                            new InstrumentationOptions
                            {
                                CaptureCommandText = true
                            }));

                    var mongoClient = new MongoClient(mongoClientSettings);
                    services.AddSingleton(mongoUrl);
                    services.AddSingleton(mongoClient);


                    services.AddTransient(provider =>
                        provider.GetService<MongoClient>()
                            .GetDatabase(provider.GetService<MongoUrl>().DatabaseName));

                    var honeycombOptions = context.Configuration.GetHoneycombOptions();

                    services.AddOpenTelemetry()
                        .WithTracing(builder =>
                        {
                            builder
                                .ConfigureResource(resource => resource.AddService(Program.EndpointName))
                                .AddSource("NServiceBus.Core")
                                .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
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
                    services.AddHostedService<Mongo2GoService>();
                });

                webHostBuilder.Configure(app =>
                {
                    app.UseOpenTelemetryPrometheusScrapingEndpoint();
                });
            })


    ;
}