using TraceLens.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddContainer("zipkin", "openzipkin/zipkin")
    .WithEndpoint(9411, 9411);

var rmqPassword = builder.AddParameter("messaging-password");
var dbPassword = builder.AddParameter("db-password");

var broker = builder.AddRabbitMQ(name: "broker", password: rmqPassword, port: 5672)
    .WithDataVolume()
    .WithManagementPlugin()
    .WithEndpoint("management", e => e.Port = 15672)
    .WithHealthCheck();
var mongo = builder.AddMongoDB("mongo");
var sql = builder.AddSqlServer("sql", password: dbPassword)
    .WithHealthCheck()
    .WithDataVolume()
    .AddDatabase("sqldata");

var serviceControlDb = builder
    .AddContainer("servicecontroldb", "particular/servicecontrol-ravendb", "latest-arm64v8")
    .WithEndpoint(8080, 8080);

var serviceControl = builder
    .AddContainer("servicecontrol", "particular/servicecontrol")
    .WithEnvironment("TransportType", "RabbitMQ.QuorumConventionalRouting")
    .WithEnvironment("ConnectionString", "host=host.docker.internal")
    .WithEnvironment("RavenDB_ConnectionString", "http://host.docker.internal:8080")
    .WithEnvironment("RemoteInstances", "[{\"api_uri\":\"http://host.docker.internal:44444/api\"}]")
    .WithEndpoint(33333, 33333)
    .WaitFor(broker);

var serviceControlAudit = builder
    .AddContainer("servicecontrolaudit", "particular/servicecontrol-audit")
    .WithEnvironment("TransportType", "RabbitMQ.QuorumConventionalRouting")
    .WithEnvironment("ConnectionString", "host=host.docker.internal")
    .WithEnvironment("RavenDB_ConnectionString", "http://host.docker.internal:8080")
    .WithEndpoint(44444, 44444)
    .WaitFor(broker);

var serviceControlMonitoring = builder
    .AddContainer("servicecontrolmonitoring", "particular/servicecontrol-monitoring")
    .WithEnvironment("TransportType", "RabbitMQ.QuorumConventionalRouting")
    .WithEnvironment("ConnectionString", "host=host.docker.internal")
    .WithEndpoint(33633, 33633)
    .WaitFor(broker);

var servicePulse = builder
    .AddContainer("servicepulse", "particular/servicepulse")
    .WithEnvironment("SERVICECONTROL_URL", "http://host.docker.internal:33333/api/")
    .WithEnvironment("MONITORING_URLS", "['http://host.docker.internal:33633/']")
    .WithEndpoint(9090, 90)
    .WaitFor(broker);

var web = builder
    .AddProject<Projects.WebApplication>("web")
    .WithReference(broker)
    .WithReference(sql)
    .WaitFor(broker)
    .WaitFor(sql);

var childWorker = builder
    .AddProject<Projects.ChildWorkerService>("childworker")
    .WithReference(broker)
    .WithReference(mongo)
    .WaitFor(broker);

var worker = builder
    .AddProject<Projects.WorkerService>("worker")
    .WithReference(broker)
    .WithReference(childWorker)
    .WithReference(web)
    .WaitFor(broker);

    

builder.Build().Run();
