using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace JobVault.Infrastructure.Persistence.MongoDB;

public class SettingsRepository : ISettingsRepository
{
    private readonly IMongoCollection<AppSettingsDocument> _collection;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SettingsRepository> _logger;

    public SettingsRepository(IConfiguration configuration, ILogger<SettingsRepository> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var connectionString = configuration["MongoDb:ConnectionString"]
            ?? throw new InvalidOperationException("MongoDb:ConnectionString is not configured");
        var databaseName = configuration["MongoDb:DatabaseName"]
            ?? throw new InvalidOperationException("MongoDb:DatabaseName is not configured");

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<AppSettingsDocument>("settings");
    }

    public async Task<AppSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = await _collection.Find(FilterDefinition<AppSettingsDocument>.Empty)
                .FirstOrDefaultAsync(cancellationToken);

            if (doc == null) return null;

            return new AppSettings
            {
                Id = doc.Id,
                GitHubOwner = Resolve(doc.GitHubOwner, _configuration["GitHub:Owner"], "k-bilaluddin"),
                GitHubRepository = Resolve(doc.GitHubRepository, _configuration["GitHub:Repository"], "job-applications-vault"),
                GitHubBranch = Resolve(doc.GitHubBranch, _configuration["GitHub:Branch"], "master"),
                GitHubCvFileName = Resolve(doc.GitHubCvFileName, _configuration["GitHub:CvFileName"], "KhawajaBilal_Uddin_CV"),
                GitHubCoverLetterFileName = Resolve(doc.GitHubCoverLetterFileName, _configuration["GitHub:CoverLetterFileName"], "KhawajaBilal_Uddin_CoverLetter"),
                TelegramChatId = Resolve(doc.TelegramChatId, _configuration["Telegram:ChatId"], ""),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading settings from MongoDB");
            return null;
        }
    }

    public async Task<AppSettings> SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var doc = new AppSettingsDocument
        {
            GitHubOwner = settings.GitHubOwner,
            GitHubRepository = settings.GitHubRepository,
            GitHubBranch = settings.GitHubBranch,
            GitHubCvFileName = settings.GitHubCvFileName,
            GitHubCoverLetterFileName = settings.GitHubCoverLetterFileName,
            TelegramChatId = settings.TelegramChatId,
            UpdatedAt = DateTime.UtcNow,
        };

        var existing = await _collection.Find(FilterDefinition<AppSettingsDocument>.Empty)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            doc.Id = existing.Id;
            await _collection.ReplaceOneAsync(
                Builders<AppSettingsDocument>.Filter.Eq(d => d.Id, existing.Id),
                doc, cancellationToken: cancellationToken);
        }
        else
        {
            doc.Id = ObjectId.GenerateNewId().ToString();
            await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }

        settings.Id = doc.Id;
        _logger.LogInformation("Settings saved to MongoDB");
        return settings;
    }

    private static string Resolve(string? dbValue, string? envValue, string defaultValue)
    {
        if (!string.IsNullOrEmpty(dbValue)) return dbValue;
        if (!string.IsNullOrEmpty(envValue)) return envValue;
        return defaultValue;
    }
}
