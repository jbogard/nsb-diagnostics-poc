using ChildWorkerService;

const string EndpointName = "NsbActivities.ChildWorkerService";

var builder = Host.CreateApplicationBuilder(args);

var endpointConfiguration = new EndpointConfiguration(EndpointName);

endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

Console.WriteLine(builder.Configuration.GetConnectionString("broker"));

var transport = new RabbitMQTransport(
    RoutingTopology.Conventional(QueueType.Quorum),
    builder.Configuration.GetConnectionString("broker")
);
endpointConfiguration.UseTransport(transport);

endpointConfiguration.UsePersistence<LearningPersistence>();

endpointConfiguration.EnableInstallers();

var recoverability = endpointConfiguration.Recoverability();
recoverability.Immediate(i => i.NumberOfRetries(1));
recoverability.Delayed(i => i.NumberOfRetries(0));

endpointConfiguration.AuditProcessedMessagesTo("audit");

endpointConfiguration.EnableOpenTelemetry();

endpointConfiguration.ConnectToServicePlatformDefaults();

builder.UseNServiceBus(endpointConfiguration);

builder.AddServiceDefaults();

builder.Services.AddHostedService<Mongo2GoService>();
builder.AddMongoDBClient("mongo");

var host = builder.Build();

host.Run();