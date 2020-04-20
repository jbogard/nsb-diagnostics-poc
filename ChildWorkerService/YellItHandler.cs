using System;
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

        private static readonly Random _coinFlip = new Random();

        public YellItHandler(ILogger<YellItHandler> logger)
            => _logger = logger;

        public Task Handle(SomethingSaid message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Yelling out {message}", message.Message);

            if (_coinFlip.Next(2) == 0)
            {
                throw new Exception("Something went wrong!");
            }

            return context.Publish(new SomethingYelled
            {
                Message = message.Message.ToUpperInvariant(),
                Id = message.Id
            });
        }

    }
}