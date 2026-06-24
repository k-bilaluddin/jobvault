using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace JobVault.Infrastructure.Persistence.MongoDB;

[BsonIgnoreExtraElements]
internal class AppSettingsDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("gitHubOwner")]
    public string? GitHubOwner { get; set; }

    [BsonElement("gitHubRepository")]
    public string? GitHubRepository { get; set; }

    [BsonElement("gitHubBranch")]
    public string? GitHubBranch { get; set; }

    [BsonElement("gitHubCvFileName")]
    public string? GitHubCvFileName { get; set; }

    [BsonElement("gitHubCoverLetterFileName")]
    public string? GitHubCoverLetterFileName { get; set; }

    [BsonElement("telegramChatId")]
    public string? TelegramChatId { get; set; }

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
