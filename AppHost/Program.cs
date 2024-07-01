var builder = DistributedApplication.CreateBuilder(args);

var broker = builder.AddRabbitMQ("broker")
    .WithManagementPlugin();
var mongo = builder.AddMongoDB("mongo");
var sql = builder.AddSqlServer("sql")
    .AddDatabase("sqldata");

var web = builder
    .AddProject<Projects.WebApplication>("web")
    .WithReference(broker)
    .WithReference(sql);

var childWorker = builder
    .AddProject<Projects.ChildWorkerService>("childworker")
    .WithReference(broker)
    .WithReference(mongo);

var worker = builder
    .AddProject<Projects.WorkerService>("worker")
    .WithReference(broker)
    .WithReference(childWorker)
    .WithReference(web);




builder.Build().Run();
