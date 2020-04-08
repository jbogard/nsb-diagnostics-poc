using System.Threading.Tasks;
using ChildWorkerService.Messages;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace ChildWorkerService
{
    public class MakeItYellHandler : IHandleMessages<MakeItYell>
    {
        private readonly ILogger<MakeItYellHandler> _logger;

        public MakeItYellHandler(ILogger<MakeItYellHandler> logger) 
            => _logger = logger;

        public Task Handle(MakeItYell message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Yelling out {message}", message.Value);

            return context.Reply(new MakeItYellResponse
            {
                Value = message.Value.ToUpperInvariant()
            });
        }
    }
}