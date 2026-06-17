using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using JobVault.Domain.ValueObjects;
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

            string id;
            if (isNew)
            {
                application.CreatedAt = DateTime.UtcNow;
                id = ObjectId.GenerateNewId().ToString();
            }
            else
            {
                application.CreatedAt = existing!["createdAt"].ToUniversalTime();
                id = existing["_id"].AsObjectId.ToString();
            }

            var doc = new BsonDocument
            {
                ["_id"] = ObjectId.Parse(id),
                ["companyName"] = application.CompanyName,
                ["jobTitle"] = application.JobTitle,
                ["location"] = application.Location ?? string.Empty,
                ["jobUrl"] = application.JobUrl ?? string.Empty,
                ["workMode"] = application.WorkMode ?? string.Empty,
                ["employmentType"] = application.EmploymentType ?? string.Empty,
                ["salaryMin"] = application.SalaryMin.HasValue ? (BsonValue)application.SalaryMin.Value : BsonNull.Value,
                ["salaryMax"] = application.SalaryMax.HasValue ? (BsonValue)application.SalaryMax.Value : BsonNull.Value,
                ["currency"] = application.Currency ?? "EUR",
                ["salaryPeriod"] = application.SalaryPeriod ?? "annual",
                ["matchScore"] = application.MatchScore,
                ["recommendation"] = application.Recommendation ?? string.Empty,
                ["status"] = application.Status ?? string.Empty,
                ["createdAt"] = application.CreatedAt,
                ["updatedAt"] = application.UpdatedAt,
                ["jdSource"] = application.JdSource != null ? (BsonValue)application.JdSource : BsonNull.Value,
                ["headline"] = application.Headline != null ? (BsonValue)application.Headline : BsonNull.Value,
                ["summary"] = application.Summary != null ? (BsonValue)application.Summary : BsonNull.Value,
                ["skills"] = new BsonArray(application.Skills.Select(s => new BsonDocument
                {
                    ["label"] = s.Label,
                    ["value"] = s.Value,
                })),
                ["roles"] = new BsonArray(application.Roles.Select(r => new BsonDocument
                {
                    ["id"] = r.Id,
                    ["bullets"] = new BsonArray(r.Bullets.Select(b => (BsonValue)b)),
                })),
                ["recipient"] = application.Recipient != null ? (BsonValue)application.Recipient : BsonNull.Value,
                ["coverLetterParagraphs"] = new BsonArray(application.CoverLetterParagraphs.Select(p => (BsonValue)p)),
                ["strengths"] = new BsonArray(application.Strengths.Select(s => (BsonValue)s)),
                ["gaps"] = new BsonArray(application.Gaps.Select(g => (BsonValue)g)),
                ["tailoringNotes"] = application.TailoringNotes != null ? (BsonValue)application.TailoringNotes : BsonNull.Value,
                ["compatibilityReportMarkdown"] = application.CompatibilityReportMarkdown != null ? (BsonValue)application.CompatibilityReportMarkdown : BsonNull.Value,
                ["tailoringNotesMarkdown"] = application.TailoringNotesMarkdown != null ? (BsonValue)application.TailoringNotesMarkdown : BsonNull.Value,
                ["commitUrl"] = application.CommitUrl != null ? (BsonValue)application.CommitUrl : BsonNull.Value,
                ["errorDetails"] = application.ErrorDetails != null ? (BsonValue)application.ErrorDetails : BsonNull.Value,
            };

            if (isNew)
            {
                await _collection.InsertOneAsync(doc);
                _logger.LogInformation("Inserted new job application for {CompanyName} (id={Id})", application.CompanyName, id);
            }
            else
            {
                await _collection.ReplaceOneAsync(filter, doc);
                _logger.LogInformation("Updated existing job application for {CompanyName} (id={Id})", application.CompanyName, id);
            }

            return UpsertResult.Success(isNew, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting job application for {CompanyName}", application.CompanyName);
            return UpsertResult.Failure();
        }
    }

    public async Task<JobApplication?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var oid))
            {
                _logger.LogWarning("Invalid ObjectId: {Id}", id);
                return null;
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", oid);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            return doc == null ? null : MapToJobApplication(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching job application by id={Id}", id);
            return null;
        }
    }

    public async Task<bool> UpdateStatusAsync(
        string id,
        string status,
        string? commitUrl = null,
        string? errorDetails = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var oid))
            {
                _logger.LogWarning("Invalid ObjectId for status update: {Id}", id);
                return false;
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", oid);

            var updateDef = Builders<BsonDocument>.Update
                .Set("status", status)
                .Set("updatedAt", DateTime.UtcNow);

            if (commitUrl != null)
                updateDef = updateDef.Set("commitUrl", commitUrl);

            if (errorDetails != null)
                updateDef = updateDef.Set("errorDetails", errorDetails);

            // Clear generation payload from MongoDB once processing is done (success or failure)
            if (status == "Ready to Apply" || status == "Failed")
            {
                updateDef = updateDef
                    .Unset("jdSource")
                    .Unset("headline")
                    .Unset("summary")
                    .Unset("skills")
                    .Unset("roles")
                    .Unset("recipient")
                    .Unset("coverLetterParagraphs")
                    .Unset("strengths")
                    .Unset("gaps")
                    .Unset("tailoringNotes")
                    .Unset("compatibilityReportMarkdown")
                    .Unset("tailoringNotesMarkdown");
            }

            var result = await _collection.UpdateOneAsync(filter, updateDef, cancellationToken: cancellationToken);

            _logger.LogInformation("Updated status to {Status} for id={Id}", status, id);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for id={Id}", id);
            return false;
        }
    }

    private static JobApplication MapToJobApplication(BsonDocument doc)
    {
        static string? NullableString(BsonDocument d, string key) =>
            d.TryGetValue(key, out var v) && v != BsonNull.Value ? v.AsString : null;

        static int? NullableInt(BsonDocument d, string key) =>
            d.TryGetValue(key, out var v) && v != BsonNull.Value ? v.AsInt32 : null;

        return new JobApplication
        {
            Id = doc["_id"].AsObjectId.ToString(),
            CompanyName = doc.GetValue("companyName", BsonNull.Value).AsString ?? string.Empty,
            JobTitle = doc.GetValue("jobTitle", BsonNull.Value).AsString ?? string.Empty,
            Location = doc.GetValue("location", BsonNull.Value).AsString ?? string.Empty,
            JobUrl = doc.GetValue("jobUrl", BsonNull.Value).AsString ?? string.Empty,
            WorkMode = doc.GetValue("workMode", BsonNull.Value).AsString ?? string.Empty,
            EmploymentType = doc.GetValue("employmentType", BsonNull.Value).AsString ?? string.Empty,
            SalaryMin = NullableInt(doc, "salaryMin"),
            SalaryMax = NullableInt(doc, "salaryMax"),
            Currency = doc.GetValue("currency", BsonNull.Value).AsString ?? string.Empty,
            SalaryPeriod = doc.GetValue("salaryPeriod", BsonNull.Value).AsString ?? string.Empty,
            MatchScore = doc.TryGetValue("matchScore", out var ms) ? ms.AsInt32 : 0,
            Recommendation = doc.GetValue("recommendation", BsonNull.Value).AsString ?? string.Empty,
            Status = doc.GetValue("status", BsonNull.Value).AsString ?? string.Empty,
            CreatedAt = doc.TryGetValue("createdAt", out var ca) ? ca.ToUniversalTime() : default,
            UpdatedAt = doc.TryGetValue("updatedAt", out var ua) ? ua.ToUniversalTime() : default,
            JdSource = NullableString(doc, "jdSource"),
            Headline = NullableString(doc, "headline"),
            Summary = NullableString(doc, "summary"),
            Skills = doc.TryGetValue("skills", out var skillsVal) && skillsVal is BsonArray skillsArr
                ? skillsArr.Select(s => new SkillRow
                {
                    Label = s.AsBsonDocument.GetValue("label", "").AsString,
                    Value = s.AsBsonDocument.GetValue("value", "").AsString,
                }).ToList()
                : [],
            Roles = doc.TryGetValue("roles", out var rolesVal) && rolesVal is BsonArray rolesArr
                ? rolesArr.Select(r => new RolePayload
                {
                    Id = r.AsBsonDocument.GetValue("id", "").AsString,
                    Bullets = r.AsBsonDocument.TryGetValue("bullets", out var bulletsVal) && bulletsVal is BsonArray bulletsArr
                        ? bulletsArr.Select(b => b.AsString).ToList()
                        : [],
                }).ToList()
                : [],
            Recipient = NullableString(doc, "recipient"),
            CoverLetterParagraphs = doc.TryGetValue("coverLetterParagraphs", out var clpVal) && clpVal is BsonArray clpArr
                ? clpArr.Select(p => p.AsString).ToList()
                : [],
            Strengths = doc.TryGetValue("strengths", out var strVal) && strVal is BsonArray strArr
                ? strArr.Select(s => s.AsString).ToList()
                : [],
            Gaps = doc.TryGetValue("gaps", out var gapsVal) && gapsVal is BsonArray gapsArr
                ? gapsArr.Select(g => g.AsString).ToList()
                : [],
            TailoringNotes = NullableString(doc, "tailoringNotes"),
            CompatibilityReportMarkdown = NullableString(doc, "compatibilityReportMarkdown"),
            TailoringNotesMarkdown = NullableString(doc, "tailoringNotesMarkdown"),
            CommitUrl = NullableString(doc, "commitUrl"),
            ErrorDetails = NullableString(doc, "errorDetails"),
        };
    }
}
