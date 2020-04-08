using System.Threading.Tasks;
using ChildWorkerService.Messages;
using Microsoft.Extensions.Logging;
using NServiceBus;
using WebApplication.Messages;

namespace ChildWorkerService
{
    public class YellItHandler : IHandleMessages<SomethingSaid>
    {
        private readonly ILogger<YellItHandler> _logger;

        public YellItHandler(ILogger<YellItHandler> logger)
            => _logger = logger;

        public Task Handle(SomethingSaid message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Yelling out {message}", message.Message);

            return context.Publish(new SomethingYelled
            {
                Message = message.Message.ToUpperInvariant(),
                Id = message.Id
            });
        }

    }
}