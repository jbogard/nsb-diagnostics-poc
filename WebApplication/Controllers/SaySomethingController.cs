using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NServiceBus;
using WebApplication.Messages;
using WorkerService.Messages;

namespace WebApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SaySomethingController : ControllerBase
    {
        private readonly ILogger<SaySomethingController> _logger;
        private readonly IMessageSession _messageSession;

        public SaySomethingController(ILogger<SaySomethingController> logger, IMessageSession messageSession)
        {
            _logger = logger;
            _messageSession = messageSession;
        }

        [HttpGet]
        public async Task<ActionResult> Get(string message)
        {
            var command = new SaySomething
            {
                Message = message,
                Id = Guid.NewGuid()
            };

            throw new Exception("Blow Up");

            _logger.LogInformation("Sending message {message} with {id}", command.Message, command.Id);

            await _messageSession.Send(command);

            return Accepted();
        }

        [HttpGet("else")]
        public async Task<ActionResult> Else(string message)
        {
            var @event = new SomethingSaid
            {
                Message = message,
                Id = Guid.NewGuid()
            };

            _logger.LogInformation("Publishing message {message} with {id}", @event.Message, @event.Id);

            await _messageSession.Publish(@event);

            return Accepted();
        }

    }
}