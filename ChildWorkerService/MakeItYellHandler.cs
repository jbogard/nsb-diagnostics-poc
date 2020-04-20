using System;
using System.Threading.Tasks;
using ChildWorkerService.Messages;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NServiceBus;

namespace ChildWorkerService
{
    public class MakeItYellHandler : IHandleMessages<MakeItYell>
    {
        private readonly ILogger<MakeItYellHandler> _logger;
        private readonly IMongoDatabase _database;

        private static readonly Random _coinFlip = new Random();

        public MakeItYellHandler(ILogger<MakeItYellHandler> logger, IMongoDatabase database)
        {
            _logger = logger;
            _database = database;
        }

        public async Task Handle(MakeItYell message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Yelling out {message}", message.Value);

            if (_coinFlip.Next(2) == 0)
            {
                throw new Exception("Something went wrong!");
            }

            var collection = _database.GetCollection<Person>(nameof(Person));

            var count = await collection.CountDocumentsAsync(p => true);
            var rng = new Random();

            var favoritePerson = await collection.AsQueryable().Skip(rng.Next((int)count)).FirstAsync();

            await context.Reply(new MakeItYellResponse
            {
                Value = message.Value.ToUpperInvariant(),
                FavoritePerson = $"{favoritePerson.FirstName} {favoritePerson.LastName}"
            });
        }
    }
}