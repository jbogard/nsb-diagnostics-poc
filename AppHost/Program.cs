var builder = DistributedApplication.CreateBuilder(args);

builder.AddContainer("zipkin", "openzipkin/zipkin")
    .WithEndpoint(9411, 9411);

var broker = builder.AddRabbitMQ("broker")
    .WithManagementPlugin()
    .WithHealthCheck();
var mongo = builder.AddMongoDB("mongo");
var sql = builder.AddSqlServer("sql")
    .WithHealthCheck()
    .AddDatabase("sqldata");

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
