using ChildWorkerService.Messages;

const string EndpointName = "NsbActivities.WorkerService";

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

//.ConfigureLogging(logging =>
//{
//    logging.AddOpenTelemetry(options =>
//    {
//        options.IncludeFormattedMessage = true;
//        options.IncludeScopes = true;
//        options.ParseStateValues = true;
//        options.AddConsoleExporter();
//        options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(EndpointName));
//    });
//})

var endpointConfiguration = new EndpointConfiguration(EndpointName);

endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

var transport = new RabbitMQTransport(
    RoutingTopology.Conventional(QueueType.Classic),
    builder.Configuration.GetConnectionString("broker")
);
var transportSettings = endpointConfiguration.UseTransport(transport);

transportSettings.RouteToEndpoint(typeof(MakeItYell).Assembly, "NsbActivities.ChildWorkerService");

endpointConfiguration.UsePersistence<LearningPersistence>();

endpointConfiguration.EnableInstallers();

endpointConfiguration.EnableOpenTelemetry();

builder.UseNServiceBus(endpointConfiguration);

builder.Services.AddHttpClient("WebApplication", client => client.BaseAddress = new Uri("https://webapplication"));

var host = builder.Build();

host.Run();

//            .ConfigureWebHostDefaults(webHostBuilder =>
//            {
//                webHostBuilder.ConfigureServices((context, services) =>
//                {
//                    services.AddScoped<Func<HttpClient>>(_ => () => new HttpClient
//                    {
//                        BaseAddress = new Uri("https://localhost:5001")
//                    });

//                    var honeycombOptions = context.Configuration.GetHoneycombOptions();

//                    services.AddOpenTelemetry()
//                        .WithTracing(builder =>
//                        {
//                            builder
//                                .ConfigureResource(resource => resource.AddService(Program.EndpointName))
//                                .AddSource("NServiceBus.Core")
//                                .AddHttpClientInstrumentation()
//                                .AddProcessor(new CopyBaggageToTagsProcessor())
//                                .AddHoneycomb(honeycombOptions)
//                                .AddZipkinExporter(o =>
//                                {
//                                    o.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
//                                })
//                                .AddJaegerExporter(c =>
//                                {
//                                    c.AgentHost = "localhost";
//                                    c.AgentPort = 6831;
//                                });
//                        })
//                        .WithMetrics(builder =>
//                        {
//                            builder
//                                .ConfigureResource(resource => resource.AddService(Program.EndpointName))
//                                .AddMeter("NServiceBus.Core")
//                                .AddPrometheusExporter(options =>
//                                {
//                                    options.ScrapeResponseCacheDurationMilliseconds = 0;
//                                });
//                        })
//                        ;
//                });
//                webHostBuilder.Configure(app =>
//                {
//                    app.UseOpenTelemetryPrometheusScrapingEndpoint();
//                });
//            })
//    ;
//}