﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ChildWorkerService.Messages;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NServiceBus;

namespace ChildWorkerService;

public class MakeItYellHandler : IHandleMessages<MakeItYell>
{
    private readonly ILogger<MakeItYellHandler> _logger;
    private readonly IMongoDatabase _database;
    private static readonly Random rng = new(Guid.NewGuid().GetHashCode());

    private static readonly Random _coinFlip = new Random();

    public MakeItYellHandler(ILogger<MakeItYellHandler> logger, IMongoDatabase database)
    {
        _logger = logger;
        _database = database;
    }

    public async Task Handle(MakeItYell message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Yelling out {message}", message.Value);

        var collection = _database.GetCollection<Person>(nameof(Person));

        var count = await collection.CountDocumentsAsync(p => true, cancellationToken: context.CancellationToken);

        var next = rng.Next((int)count);

        if (context.Extensions.TryGet(out Activity currentActivity))
        {
            currentActivity.AddTag("code.randomvalue", next);
        }

        var favoritePerson = await collection.AsQueryable().Skip(next).FirstAsync(cancellationToken: context.CancellationToken);

        // add random jitter
        await Task.Delay(rng.Next() % 1000, cancellationToken: context.CancellationToken);

        await context.Reply(new MakeItYellResponse
        {
            Value = message.Value.ToUpperInvariant(),
            FavoritePerson = $"{favoritePerson.FirstName} {favoritePerson.LastName}"
        });
    }
}