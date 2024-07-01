using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using WebApplication;
using WorkerService.Messages;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();

const string EndpointName = "NsbActivities.WebApplication";

var endpointConfiguration = new EndpointConfiguration(EndpointName);

var transport = new RabbitMQTransport(
    RoutingTopology.Conventional(QueueType.Classic),
    builder.Configuration.GetConnectionString("broker")
);
var transportSettings = endpointConfiguration.UseTransport(transport);

Thread.Sleep(10000);

transportSettings.RouteToEndpoint(typeof(SaySomething).Assembly, "NsbActivities.WorkerService");

endpointConfiguration.UsePersistence<LearningPersistence>();
endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

endpointConfiguration.EnableInstallers();

endpointConfiguration.EnableOpenTelemetry();

builder.UseNServiceBus(endpointConfiguration);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddSqlServerDbContext<WeatherContext>("sqldata");

builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<DbInitializer>();

var app = builder.Build();

app.Logger.LogInformation(builder.Configuration.GetConnectionString("broker"));
app.Logger.LogInformation(builder.Configuration.GetConnectionString("sqldata"));

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days.
    // You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


//public class Program
//{
//    public const string EndpointName = "NsbActivities.WebApplication";

//    public static void Main(string[] args)
//    {
//        var host = CreateHostBuilder(args).Build();

//        SeedDb(host);

//        host.Run();
//    }

//    private static void SeedDb(IHost host)
//    {

//    }

//    public static IHostBuilder CreateHostBuilder(string[] args) =>
//        Host.CreateDefaultBuilder(args)
//            .ConfigureLogging(logging =>
//            {
//                //logging.AddOpenTelemetry(options =>
//                //{
//                //    options.IncludeFormattedMessage = true;
//                //    options.IncludeScopes = true;
//                //    options.ParseStateValues = true;
//                //    options.AddConsoleExporter();
//                //    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(EndpointName));
//                //});
//            })
//            .UseNServiceBus(context =>
//            {

//                // configure endpoint here
//                return endpointConfiguration;
//            })
//            .ConfigureWebHostDefaults(webBuilder =>
//            {
//                webBuilder.UseStartup<Startup>();
//            })
//    ;
//}