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
    public class GetTemperatureHandler : IHandleMessages<GetTemperature>
    {
        private readonly ILogger<GetTemperatureHandler> _logger;

        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:5001")
        };


        public GetTemperatureHandler(ILogger<GetTemperatureHandler> logger)
            => _logger = logger;

        public async Task Handle(GetTemperature message, IMessageHandlerContext context)
        {
            var content = await _httpClient.GetStringAsync("/weatherforecast/today");

            dynamic json = Deserialize<ExpandoObject>(content);

            var temp = (int)json.temperatureF.GetInt32();

            await context.Reply(new GetTemperatureResponse
            {
                Value = temp
            });
        }
    }
}