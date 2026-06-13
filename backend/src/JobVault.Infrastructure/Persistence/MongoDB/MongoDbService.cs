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

            var applications = documents.Select(MapBsonToApplication).ToList();

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

    public async Task<JobApplication?> GetApplicationByIdAsync(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                _logger.LogWarning("Invalid ObjectId format: {Id}", id);
                return null;
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            var document = await _collection.Find(filter).FirstOrDefaultAsync();

            if (document == null)
            {
                _logger.LogInformation("Application not found with ID: {Id}", id);
                return null;
            }

            return MapBsonToApplication(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application by ID: {Id}", id);
            return null;
        }
    }

    public async Task<JobApplication?> UpdateApplicationStatusAsync(string id, string newStatus)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                _logger.LogWarning("Invalid ObjectId format: {Id}", id);
                return null;
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            var update = Builders<BsonDocument>.Update
                .Set("status", newStatus)
                .Set("updatedAt", DateTime.UtcNow);

            var options = new FindOneAndUpdateOptions<BsonDocument>
            {
                ReturnDocument = ReturnDocument.After
            };

            var updatedDocument = await _collection.FindOneAndUpdateAsync(filter, update, options);

            if (updatedDocument == null)
            {
                _logger.LogWarning("Application not found with ID: {Id}", id);
                return null;
            }

            _logger.LogInformation("Updated status for application {Id} to {Status}", id, newStatus);
            return MapBsonToApplication(updatedDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for application {Id}", id);
            return null;
        }
    }

    public async Task<(int TotalCount, Dictionary<string, int> StatusCounts, double AverageScore)> GetDashboardStatsAsync()
    {
        try
        {
            var pipeline = new[]
            {
                new BsonDocument("$facet", new BsonDocument
                {
                    ["totalCount"] = new BsonArray { new BsonDocument("$count", "count") },
                    ["statusCounts"] = new BsonArray
                    {
                        new BsonDocument("$group", new BsonDocument
                        {
                            ["_id"] = "$status",
                            ["count"] = new BsonDocument("$sum", 1)
                        })
                    },
                    ["averageScore"] = new BsonArray
                    {
                        new BsonDocument("$group", new BsonDocument
                        {
                            ["_id"] = BsonNull.Value,
                            ["avg"] = new BsonDocument("$avg", "$matchScore")
                        })
                    }
                })
            };

            var results = await _collection.AggregateAsync<BsonDocument>(pipeline);
            var result = await results.FirstOrDefaultAsync();

            if (result == null)
            {
                return (0, new Dictionary<string, int>(), 0.0);
            }

            var totalCount = result["totalCount"].AsBsonArray.Count > 0
                ? result["totalCount"][0]["count"].AsInt32
                : 0;

            var statusCounts = new Dictionary<string, int>();
            foreach (var item in result["statusCounts"].AsBsonArray)
            {
                var status = item["_id"].AsString;
                var count = item["count"].AsInt32;
                statusCounts[status] = count;
            }

            var averageScore = result["averageScore"].AsBsonArray.Count > 0
                ? result["averageScore"][0]["avg"].ToDouble()
                : 0.0;

            _logger.LogInformation("Retrieved dashboard stats: Total={Total}, Avg Score={AvgScore}", totalCount, averageScore);
            return (totalCount, statusCounts, averageScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard stats");
            return (0, new Dictionary<string, int>(), 0.0);
        }
    }

    public async Task<(List<JobApplication> Applications, long TotalCount)> GetRecentActivityAsync(int page, int pageSize)
    {
        try
        {
            var filter = Builders<BsonDocument>.Filter.Empty;
            var totalCount = await _collection.CountDocumentsAsync(filter);

            var skip = (page - 1) * pageSize;
            var documents = await _collection
                .Find(filter)
                .Sort(Builders<BsonDocument>.Sort.Descending("updatedAt"))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            var applications = documents.Select(MapBsonToApplication).ToList();

            _logger.LogInformation("Retrieved {Count} recent activities (page {Page})", applications.Count, page);
            return (applications, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent activity");
            return (new List<JobApplication>(), 0);
        }
    }

    private static JobApplication MapBsonToApplication(BsonDocument doc)
    {
        return new JobApplication
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
        };
    }
}
