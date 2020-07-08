using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Extensions.IntegrationTesting;
using WorkerService;
using Xunit;

namespace IntegrationTests
{
    public class SystemFixture : IDisposable
    {
        public SystemFixture()
        {
            ChildWorkerHost = new ChildWorkerServiceFactory();
            WorkerHost = new WorkerServiceFactory()
                .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<Func<HttpClient>>(s => () => WebAppHost.CreateClient());
                }));
            WebAppHost = new WebAppFactory();
            EndpointFixture = new EndpointFixture();
        }

        public WebAppFactory WebAppHost { get; }

        public WebApplicationFactory<Program> WorkerHost { get; }

        public ChildWorkerServiceFactory ChildWorkerHost { get; }

        public EndpointFixture EndpointFixture { get; }

        public void Start()
        {
            WorkerHost.CreateClient();
            WebAppHost.CreateClient();
            ChildWorkerHost.CreateClient();
        }

        public void Dispose()
        {
            WebAppHost?.Dispose();
            WorkerHost?.Dispose();
            ChildWorkerHost?.Dispose();
            EndpointFixture?.Dispose();
        }
    }

    [CollectionDefinition(nameof(SystemCollection), DisableParallelization = true)]
    public class SystemCollection : ICollectionFixture<SystemFixture> { }
}