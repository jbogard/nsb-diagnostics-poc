using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mongo2Go;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.OpenTelemetry;
using MongoDB.Driver.Core.Extensions.SystemDiagnostics;
using NServiceBus;
using NServiceBus.Diagnostics.OpenTelemetry;
using NServiceBus.Json;
using OpenTelemetry.Trace.Configuration;

namespace ChildWorkerService
{
    public class Program
    {
        private const string EndpointName = "NsbActivities.ChildWorkerService";

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

                    endpointConfiguration.UsePersistence<LearningPersistence>();

                    endpointConfiguration.EnableInstallers();

                    endpointConfiguration.AuditProcessedMessagesTo("NsbActivities.Audit");

                    endpointConfiguration.PurgeOnStartup(true);

                    var recoverability = endpointConfiguration.Recoverability();
                    recoverability.Immediate(i => i.NumberOfRetries(1));
                    recoverability.Delayed(i => i.NumberOfRetries(0));

                    // configure endpoint here
                    return endpointConfiguration;
                })
                .ConfigureServices((context, services) =>
                {
                    var runner = MongoDbRunner.Start(singleNodeReplSet: true, singleNodeReplSetWaitTimeout: 20);
                    
                    services.AddSingleton(runner);
                    var urlBuilder = new MongoUrlBuilder(runner.ConnectionString)
                    {
                        DatabaseName = "dev"
                    };
                    var mongoUrl = urlBuilder.ToMongoUrl();
                    var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);
                    mongoClientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
                    var mongoClient = new MongoClient(mongoClientSettings);
                    services.AddSingleton(mongoUrl);
                    services.AddSingleton(mongoClient);
                    services.AddTransient(provider => provider.GetService<MongoClient>().GetDatabase(provider.GetService<MongoUrl>().DatabaseName));
                    services.AddHostedService<Mongo2GoService>();
                    services.AddOpenTelemetry(builder =>
                    {
                        builder
                            .UseZipkin(o =>
                            {
                                o.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
                                o.ServiceName = EndpointName;
                            })
                            .UseJaeger(c =>
                            {
                                c.AgentHost = "localhost";
                                c.AgentPort = 6831;
                                c.ServiceName = EndpointName;
                            })
                            .UseApplicationInsights(c =>
                            {
                                c.InstrumentationKey = context.Configuration.GetValue<string>("ApplicationInsights:InstrumentationKey");
                            })
                            .AddNServiceBusAdapter()
                            .AddMongoDBAdapter()
                            .AddRequestAdapter()
                            .AddDependencyAdapter();
                    });
                })
        ;
    }
}
