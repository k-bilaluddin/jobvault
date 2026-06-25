using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace JobVault.Infrastructure.Persistence.MongoDB;

public class PendingJobRepository : IPendingJobRepository
{
    private readonly IMongoCollection<PendingJobDocument> _collection;
    private readonly ILogger<PendingJobRepository> _logger;

    public PendingJobRepository(IConfiguration configuration, ILogger<PendingJobRepository> logger)
    {
        _logger = logger;

        var connectionString = configuration["MongoDb:ConnectionString"]
            ?? throw new InvalidOperationException("MongoDb:ConnectionString is not configured");
        var databaseName = configuration["MongoDb:DatabaseName"]
            ?? throw new InvalidOperationException("MongoDb:DatabaseName is not configured");
        var collectionName = configuration["MongoDb:PendingJobsCollectionName"] ?? "pending_jobs";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<PendingJobDocument>(collectionName);
    }

    public async Task<PendingJob> CreateAsync(string url, CancellationToken ct = default)
    {
        var doc = new PendingJobDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Url = url,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _collection.InsertOneAsync(doc, cancellationToken: ct);
        _logger.LogInformation("Created pending job {Id} for {Url}", doc.Id, url);
        return ToEntity(doc);
    }

    public async Task<IReadOnlyList<PendingJob>> GetAllAsync(string? status = null, CancellationToken ct = default)
    {
        var filter = status != null
            ? Builders<PendingJobDocument>.Filter.Eq(d => d.Status, status)
            : FilterDefinition<PendingJobDocument>.Empty;

        var docs = await _collection
            .Find(filter)
            .Sort(Builders<PendingJobDocument>.Sort.Ascending(d => d.CreatedAt))
            .ToListAsync(ct);

        return docs.Select(ToEntity).ToList();
    }

    public async Task<IReadOnlyList<PendingJob>> GetPendingAsync(CancellationToken ct = default)
    {
        return await GetAllAsync("pending", ct);
    }

    public async Task<PendingJob?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<PendingJobDocument>.Filter.Eq(d => d.Id, id);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);
        return doc == null ? null : ToEntity(doc);
    }

    public async Task<bool> UpdateAsync(string id, string? url, string? status, CancellationToken ct = default)
    {
        var filter = Builders<PendingJobDocument>.Filter.Eq(d => d.Id, id);
        var update = Builders<PendingJobDocument>.Update.Set(d => d.UpdatedAt, DateTime.UtcNow);

        if (url != null) update = update.Set(d => d.Url, url);
        if (status != null) update = update.Set(d => d.Status, status);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<PendingJobDocument>.Filter.Eq(d => d.Id, id);
        var result = await _collection.DeleteOneAsync(filter, ct);
        return result.DeletedCount > 0;
    }

    public async Task<bool> SetStatusAsync(string id, string status, CancellationToken ct = default)
    {
        return await UpdateAsync(id, null, status, ct);
    }

    public async Task<int> DeleteByStatusAsync(string status, CancellationToken ct = default)
    {
        var filter = Builders<PendingJobDocument>.Filter.Eq(d => d.Status, status);
        var result = await _collection.DeleteManyAsync(filter, ct);
        return (int)result.DeletedCount;
    }

    private static PendingJob ToEntity(PendingJobDocument doc) => new()
    {
        Id = doc.Id,
        Url = doc.Url,
        Status = doc.Status,
        CreatedAt = doc.CreatedAt,
        UpdatedAt = doc.UpdatedAt,
    };
}
