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

    public async Task<(List<JobApplication> Applications, long TotalCount)> GetApplicationsAsync(
        int page,
        int pageSize,
        string? status = null,
        string? company = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? minScore = null,
        int? maxScore = null)
    {
        try
        {
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filters = new List<FilterDefinition<BsonDocument>>();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(status))
            {
                filters.Add(filterBuilder.Eq("status", status));
            }

            if (!string.IsNullOrWhiteSpace(company))
            {
                filters.Add(filterBuilder.Regex("companyName", new BsonRegularExpression(company, "i")));
            }

            if (fromDate.HasValue)
            {
                filters.Add(filterBuilder.Gte("createdAt", fromDate.Value));
            }

            if (toDate.HasValue)
            {
                filters.Add(filterBuilder.Lte("createdAt", toDate.Value.AddDays(1).AddTicks(-1)));
            }

            if (minScore.HasValue)
            {
                filters.Add(filterBuilder.Gte("matchScore", minScore.Value));
            }

            if (maxScore.HasValue)
            {
                filters.Add(filterBuilder.Lte("matchScore", maxScore.Value));
            }

            var combinedFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : filterBuilder.Empty;

            // Get total count
            var totalCount = await _collection.CountDocumentsAsync(combinedFilter);

            // Get paginated results
            var skip = (page - 1) * pageSize;
            var documents = await _collection
                .Find(combinedFilter)
                .Sort(Builders<BsonDocument>.Sort.Descending("createdAt"))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            var applications = documents.Select(doc => new JobApplication
            {
                Id = doc.Contains("_id") ? doc["_id"].ToString() : null,
                CompanyName = doc["companyName"].AsString,
                JobTitle = doc["jobTitle"].AsString,
                Location = doc["location"].AsString,
                JobUrl = doc["jobUrl"].AsString,
                WorkMode = doc["workMode"].AsString,
                EmploymentType = doc["employmentType"].AsString,
                SalaryMin = doc["salaryMin"].IsBsonNull ? null : doc["salaryMin"].AsInt32,
                SalaryMax = doc["salaryMax"].IsBsonNull ? null : doc["salaryMax"].AsInt32,
                Currency = doc["currency"].AsString,
                SalaryPeriod = doc["salaryPeriod"].AsString,
                MatchScore = doc["matchScore"].AsInt32,
                Recommendation = doc["recommendation"].AsString,
                Status = doc["status"].AsString,
                CreatedAt = doc["createdAt"].ToUniversalTime(),
                UpdatedAt = doc["updatedAt"].ToUniversalTime()
            }).ToList();

            _logger.LogInformation(
                "Retrieved {Count} applications (page {Page}/{TotalPages})",
                applications.Count,
                page,
                Math.Ceiling(totalCount / (double)pageSize));

            return (applications, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications");
            return (new List<JobApplication>(), 0);
        }
    }
}
