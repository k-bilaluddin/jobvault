using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace JobVault.Infrastructure.Persistence.MongoDB;

[BsonIgnoreExtraElements]
internal class RoutineTriggerStateDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("lastTriggeredAt")]
    public DateTime? LastTriggeredAt { get; set; }
}
