using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace JobVault.Infrastructure.Persistence.MongoDB;

public class NotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(IConfiguration configuration, ILogger<NotificationRepository> logger)
    {
        _logger = logger;

        var connectionString = configuration["MongoDb:ConnectionString"]
            ?? throw new InvalidOperationException("MongoDb:ConnectionString is not configured");

        var databaseName = configuration["MongoDb:DatabaseName"]
            ?? throw new InvalidOperationException("MongoDb:DatabaseName is not configured");

        var collectionName = configuration["MongoDb:NotificationsCollectionName"] ?? "notifications";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<BsonDocument>(collectionName);

        EnsureIndexes();

        _logger.LogInformation("NotificationRepository initialized with collection: {Collection}", collectionName);
    }

    private void EnsureIndexes()
    {
        var sortIndex = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Descending("occurredAt"));

        var ttlIndex = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("occurredAt"),
            new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(30) });

        _collection.Indexes.CreateMany([sortIndex, ttlIndex]);
    }

    public async Task SaveAsync(AppNotification notification)
    {
        try
        {
            var doc = new BsonDocument
            {
                ["_id"] = notification.Id.ToString(),
                ["type"] = notification.Type,
                ["title"] = notification.Title,
                ["body"] = notification.Body,
                ["companyName"] = notification.CompanyName is not null
                    ? (BsonValue)notification.CompanyName
                    : BsonNull.Value,
                ["companySlug"] = notification.CompanySlug is not null
                    ? (BsonValue)notification.CompanySlug
                    : BsonNull.Value,
                ["occurredAt"] = notification.OccurredAt,
                ["read"] = notification.Read
            };

            await _collection.InsertOneAsync(doc);
            _logger.LogInformation("Saved notification {Id} of type {Type}", notification.Id, notification.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save notification {Id}", notification.Id);
        }
    }

    public async Task<IEnumerable<AppNotification>> GetRecentAsync(int count = 50)
    {
        try
        {
            var docs = await _collection
                .Find(FilterDefinition<BsonDocument>.Empty)
                .Sort(Builders<BsonDocument>.Sort.Descending("occurredAt"))
                .Limit(count)
                .ToListAsync();

            return docs.Select(MapToNotification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent notifications");
            return [];
        }
    }

    public async Task MarkAllReadAsync()
    {
        try
        {
            var filter = Builders<BsonDocument>.Filter.Eq("read", false);
            var update = Builders<BsonDocument>.Update.Set("read", true);
            await _collection.UpdateManyAsync(filter, update);
            _logger.LogInformation("Marked all notifications as read");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read");
        }
    }

    public async Task MarkReadAsync(Guid id)
    {
        try
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id.ToString());
            var update = Builders<BsonDocument>.Update.Set("read", true);
            await _collection.UpdateOneAsync(filter, update);
            _logger.LogInformation("Marked notification {Id} as read", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {Id} as read", id);
        }
    }

    private static AppNotification MapToNotification(BsonDocument doc) => new()
    {
        Id = Guid.TryParse(doc["_id"].AsString, out var id) ? id : Guid.NewGuid(),
        Type = doc["type"].AsString,
        Title = doc["title"].AsString,
        Body = doc["body"].AsString,
        CompanyName = doc["companyName"].IsBsonNull ? null : doc["companyName"].AsString,
        CompanySlug = doc["companySlug"].IsBsonNull ? null : doc["companySlug"].AsString,
        OccurredAt = doc["occurredAt"].ToUniversalTime(),
        Read = doc["read"].AsBoolean
    };
}
