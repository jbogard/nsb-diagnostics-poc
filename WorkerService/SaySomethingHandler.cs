using System;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using WorkerService.Messages;
using static System.Text.Json.JsonSerializer;

namespace WorkerService
{
    public class SaySomethingHandler : IHandleMessages<SaySomething>
    {
        private readonly ILogger<SaySomethingHandler> _logger;

        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:5001")
        };


        public SaySomethingHandler(ILogger<SaySomethingHandler> logger) 
            => _logger = logger;

        public async Task Handle(SaySomething message, IMessageHandlerContext context)
        {
            var content = await _httpClient.GetStringAsync("/weatherforecast/today");

            dynamic json = Deserialize<ExpandoObject>(content);

            var temp = (int)json.temperatureF.GetInt32();

            _logger.LogInformation("Saying {message} and the weather today is {weather}F", message.Message, temp);

            await context.Reply(new SaySomethingResponse
            {
                Message = $"Back at ya {message.Message}"
            });
        }
    }
}