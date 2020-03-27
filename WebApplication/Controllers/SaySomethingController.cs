using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NServiceBus;
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
                Message = message
            };

            _logger.LogInformation("Sending message {message}", command.Message);

            await _messageSession.Send(command);

            return Accepted();
        }

    }
}