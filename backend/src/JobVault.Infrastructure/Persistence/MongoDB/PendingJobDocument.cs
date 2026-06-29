using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace JobVault.Infrastructure.Persistence.MongoDB;

public class PendingJobDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "pending";

    [BsonElement("prompt")]
    public string? Prompt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
