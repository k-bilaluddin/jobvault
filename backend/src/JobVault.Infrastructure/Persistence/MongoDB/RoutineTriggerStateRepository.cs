using JobVault.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace JobVault.Infrastructure.Persistence.MongoDB;

/// <summary>Single-row collection tracking when the job-queue routine last fired — same "one row, no tenant scoping" shape as SettingsRepository.</summary>
public class RoutineTriggerStateRepository : IRoutineTriggerStateStore
{
    private readonly IMongoCollection<RoutineTriggerStateDocument> _collection;
    private readonly ILogger<RoutineTriggerStateRepository> _logger;

    public RoutineTriggerStateRepository(IConfiguration configuration, ILogger<RoutineTriggerStateRepository> logger)
    {
        _logger = logger;

        var connectionString = configuration["MongoDb:ConnectionString"]
            ?? throw new InvalidOperationException("MongoDb:ConnectionString is not configured");
        var databaseName = configuration["MongoDb:DatabaseName"]
            ?? throw new InvalidOperationException("MongoDb:DatabaseName is not configured");

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<RoutineTriggerStateDocument>("routineTriggerState");
    }

    public async Task<DateTime?> GetLastTriggeredAtAsync(CancellationToken ct = default)
    {
        var doc = await _collection.Find(FilterDefinition<RoutineTriggerStateDocument>.Empty)
            .FirstOrDefaultAsync(ct);
        return doc?.LastTriggeredAt;
    }

    public async Task SetLastTriggeredAtAsync(DateTime triggeredAtUtc, CancellationToken ct = default)
    {
        var existing = await _collection.Find(FilterDefinition<RoutineTriggerStateDocument>.Empty)
            .FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            existing.LastTriggeredAt = triggeredAtUtc;
            await _collection.ReplaceOneAsync(
                Builders<RoutineTriggerStateDocument>.Filter.Eq(d => d.Id, existing.Id),
                existing, cancellationToken: ct);
        }
        else
        {
            await _collection.InsertOneAsync(new RoutineTriggerStateDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                LastTriggeredAt = triggeredAtUtc,
            }, cancellationToken: ct);
        }

        _logger.LogInformation("Routine last-triggered-at set to {TriggeredAt:o}", triggeredAtUtc);
    }
}
