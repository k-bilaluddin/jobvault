using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace JobVault.Infrastructure.Persistence.MongoDB;

public class MongoDbService : IJobApplicationRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger<MongoDbService> _logger;

    public MongoDbService(IConfiguration configuration, ILogger<MongoDbService> logger)
    {
        _logger = logger;

        var connectionString = configuration["MongoDb:ConnectionString"]
            ?? throw new InvalidOperationException("MongoDb:ConnectionString is not configured");

        var databaseName = configuration["MongoDb:DatabaseName"]
            ?? throw new InvalidOperationException("MongoDb:DatabaseName is not configured");

        var collectionName = configuration["MongoDb:JobApplicationsCollectionName"]
            ?? throw new InvalidOperationException("MongoDb:JobApplicationsCollectionName is not configured");

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<BsonDocument>(collectionName);

        _logger.LogInformation("MongoDbService initialized with database: {Database}, collection: {Collection}",
            databaseName, collectionName);
    }

    public async Task<UpsertResult> UpsertApplicationAsync(JobApplication application)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(application.CompanyName))
            {
                _logger.LogWarning("Cannot upsert application with empty CompanyName");
                return UpsertResult.Failure();
            }

            var filter = Builders<BsonDocument>.Filter.Eq("companyName", application.CompanyName);
            var existing = await _collection.Find(filter).FirstOrDefaultAsync();

            application.UpdatedAt = DateTime.UtcNow;
            var isNew = existing == null;

            if (isNew)
            {
                application.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                application.CreatedAt = existing!["createdAt"].ToUniversalTime();
            }

            var doc = new BsonDocument
            {
                ["companyName"] = application.CompanyName,
                ["jobTitle"] = application.JobTitle,
                ["location"] = application.Location ?? "",
                ["jobUrl"] = application.JobUrl ?? "",
                ["workMode"] = application.WorkMode ?? "",
                ["employmentType"] = application.EmploymentType ?? "",
                ["salaryMin"] = application.SalaryMin.HasValue ? (BsonValue)application.SalaryMin.Value : BsonNull.Value,
                ["salaryMax"] = application.SalaryMax.HasValue ? (BsonValue)application.SalaryMax.Value : BsonNull.Value,
                ["currency"] = application.Currency ?? "EUR",
                ["salaryPeriod"] = application.SalaryPeriod ?? "annual",
                ["matchScore"] = application.MatchScore,
                ["recommendation"] = application.Recommendation ?? "",
                ["status"] = application.Status ?? "",
                ["createdAt"] = application.CreatedAt,
                ["updatedAt"] = application.UpdatedAt
            };

            if (isNew)
            {
                await _collection.InsertOneAsync(doc);
                _logger.LogInformation("Inserted new job application for {CompanyName}", application.CompanyName);
            }
            else
            {
                await _collection.ReplaceOneAsync(filter, doc);
                _logger.LogInformation("Updated existing job application for {CompanyName}", application.CompanyName);
            }

            return UpsertResult.Success(isNew);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting job application for {CompanyName}", application.CompanyName);
            return UpsertResult.Failure();
        }
    }
}
