using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using WorkerService.Messages;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    [Collection(nameof(SystemCollection))]
    public class EndToEndTests : XunitContextBase
    {
        private readonly SystemFixture _fixture;

        public EndToEndTests(SystemFixture fixture, ITestOutputHelper output) : base(output)
        {
            _fixture = fixture;
            _fixture.Start();
        }

        [Fact]
        public async Task Should_work()
        {
            var client = _fixture.WebAppHost.CreateClient();

            var message = Guid.NewGuid().ToString();

            var response =
                await _fixture.EndpointFixture.ExecuteAndWaitForHandled<SaySomethingResponse>(() =>
                    client.GetAsync($"saysomething?message={message}"), TimeSpan.FromSeconds(30));

            var saySomethingResponses = response.ReceivedMessages.OfType<SaySomethingResponse>().ToList();
            saySomethingResponses.Count.ShouldBe(1);
            saySomethingResponses[0].Message.ShouldContain(message);
        }

        [Fact]
        public async Task Should_also_work()
        {
            var client = _fixture.WebAppHost.CreateClient();

            var message = Guid.NewGuid().ToString();

            var response =
                await _fixture.EndpointFixture.ExecuteAndWaitForHandled<SomethingSaidCompleted>(() =>
                    client.GetAsync($"saysomething/else?message={message}"), TimeSpan.FromSeconds(30));

            var messages = response.ReceivedMessages.OfType<SomethingSaidCompleted>().ToList();

            messages.Count.ShouldBe(1);
            messages[0].Message.ShouldContain(message);
        }
    }
}
