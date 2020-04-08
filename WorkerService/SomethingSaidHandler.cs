using System;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
using NServiceBus;
using WebApplication.Messages;
using static System.Text.Json.JsonSerializer;
using WorkerService.Messages;

namespace WorkerService
{
    public class SomethingSaidHandler : IHandleMessages<SomethingSaid>
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:5001")
        };

        public async Task Handle(SomethingSaid message, IMessageHandlerContext context)
        {
            var content = await _httpClient.GetStringAsync("/weatherforecast/today");

            dynamic json = Deserialize<ExpandoObject>(content);

            var temp = (int)json.temperatureF.GetInt32();

            await context.Publish(new TemperatureRead
            {
                Id = message.Id,
                Value = temp
            });
        }
    }
}