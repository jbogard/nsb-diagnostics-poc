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
        private readonly Func<HttpClient> _httpClientFactory;

        public GetTemperatureHandler(ILogger<GetTemperatureHandler> logger, Func<HttpClient> httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task Handle(GetTemperature message, IMessageHandlerContext context)
        {
            var httpClient = _httpClientFactory();

            var content = await httpClient.GetStringAsync("/weatherforecast/today");

            dynamic json = Deserialize<ExpandoObject>(content);

            var temp = (int)json.temperatureF.GetInt32();

            await context.Reply(new GetTemperatureResponse
            {
                Value = temp
            });
        }
    }
}