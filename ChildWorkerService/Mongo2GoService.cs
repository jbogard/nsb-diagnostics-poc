using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Mongo2Go;
using MongoDB.Driver;

namespace ChildWorkerService
{
    public class Mongo2GoService : IHostedService
    {
        private MongoDbRunner _runner;
        private readonly IMongoDatabase _database;

        public Mongo2GoService(MongoDbRunner runner, IMongoDatabase database)
        {
            _runner = runner;
            _database = database;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var collection = _database.GetCollection<Person>(nameof(Person));

            return collection.InsertManyAsync(new[]
            {
                new Person
                {
                    FirstName = "Homer",
                    LastName = "Simpson"
                }, 
                new Person
                {
                    FirstName = "Marge",
                    LastName = "Simpson"
                }, 
                new Person
                {
                    FirstName = "Bart",
                    LastName = "Simpson"
                }, 
                new Person
                {
                    FirstName = "Lisa",
                    LastName = "Simpson"
                }, 
                new Person
                {
                    FirstName = "Maggie",
                    LastName = "Simpson"
                }, 
            }, cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _runner.Dispose();
            _runner = null;
            return Task.CompletedTask;
        }
    }
}