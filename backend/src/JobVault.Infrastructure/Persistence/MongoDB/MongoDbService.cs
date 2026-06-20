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
                ["isHistorical"] = application.IsHistorical,
            };

            // Preserve existing tracker fields on re-ingestion, set defaults on new documents
            if (isNew)
            {
                doc["stage"] = application.Stage.Length > 0 ? application.Stage : "Ready to Apply";
                doc["applied"] = application.Applied;
                doc["appliedDate"] = application.AppliedDate.HasValue ? (BsonValue)application.AppliedDate.Value : BsonNull.Value;
                doc["personalNotes"] = application.PersonalNotes;
                doc["interviews"] = MapInterviewsToBson(application.Interviews);
                doc["salary"] = MapSalaryToBson(application.Salary);
                doc["recruiter"] = MapRecruiterToBson(application.Recruiter);
                doc["followUpDate"] = application.FollowUpDate.HasValue ? (BsonValue)application.FollowUpDate.Value : BsonNull.Value;
                doc["source"] = application.Source;

                await _collection.InsertOneAsync(doc);
                _logger.LogInformation("Inserted new job application for {CompanyName} (id={Id})", application.CompanyName, id);
            }
            else
            {
                // Keep existing tracker fields from the database
                doc["stage"] = existing!.GetValue("stage", "Ready to Apply");
                doc["applied"] = existing.GetValue("applied", false);
                doc["appliedDate"] = existing.GetValue("appliedDate", BsonNull.Value);
                doc["personalNotes"] = existing.GetValue("personalNotes", "");
                doc["interviews"] = existing.GetValue("interviews", new BsonArray());
                doc["salary"] = existing.GetValue("salary", MapSalaryToBson(new SalaryInfo()));
                doc["recruiter"] = existing.GetValue("recruiter", MapRecruiterToBson(new RecruiterInfo()));
                doc["followUpDate"] = existing.GetValue("followUpDate", BsonNull.Value);
                doc["source"] = existing.GetValue("source", "");
                doc["isHistorical"] = existing.GetValue("isHistorical", false);

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
            // Keep compatibilityReportMarkdown and tailoringNotesMarkdown for serving via API
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
                    .Unset("tailoringNotes");
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

    public async Task<IReadOnlyList<JobApplication>> GetAllApplicationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var docs = await _collection.Find(FilterDefinition<BsonDocument>.Empty)
                .Sort(Builders<BsonDocument>.Sort.Descending("updatedAt"))
                .ToListAsync(cancellationToken);

            return docs.Select(MapToJobApplication).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all job applications");
            return [];
        }
    }

    public async Task<JobApplication?> GetByCompanyNameAsync(string companyName, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<BsonDocument>.Filter.Eq("companyName", companyName);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            return doc == null ? null : MapToJobApplication(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching job application by companyName={CompanyName}", companyName);
            return null;
        }
    }

    private static BsonArray MapInterviewsToBson(List<InterviewRecord> interviews) =>
        new(interviews.Select(i => new BsonDocument
        {
            ["id"] = i.Id,
            ["date"] = i.Date,
            ["type"] = i.Type,
            ["notes"] = i.Notes,
            ["outcome"] = i.Outcome,
        }));

    private static BsonDocument MapSalaryToBson(SalaryInfo salary) => new()
    {
        ["advertised"] = salary.Advertised,
        ["target"] = salary.Target,
        ["discussed"] = salary.Discussed,
        ["offered"] = salary.Offered,
    };

    private static BsonDocument MapRecruiterToBson(RecruiterInfo recruiter) => new()
    {
        ["name"] = recruiter.Name,
        ["email"] = recruiter.Email,
        ["linkedin"] = recruiter.LinkedIn,
    };

    private static JobApplication MapToJobApplication(BsonDocument doc)
    {
        static string? NullableString(BsonDocument d, string key) =>
            d.TryGetValue(key, out var v) && v != BsonNull.Value ? v.AsString : null;

        static int? NullableInt(BsonDocument d, string key) =>
            d.TryGetValue(key, out var v) && v != BsonNull.Value ? v.AsInt32 : null;

        return new JobApplication
        {
            Id = doc["_id"].BsonType == BsonType.ObjectId
                ? doc["_id"].AsObjectId.ToString()
                : doc["_id"].AsString,
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
            Stage = doc.GetValue("stage", "").AsString,
            Applied = doc.TryGetValue("applied", out var appliedVal) && appliedVal.IsBoolean && appliedVal.AsBoolean,
            AppliedDate = doc.TryGetValue("appliedDate", out var adVal) && adVal != BsonNull.Value
                ? adVal.ToUniversalTime() : null,
            PersonalNotes = doc.GetValue("personalNotes", "").AsString,
            Interviews = doc.TryGetValue("interviews", out var ivVal) && ivVal is BsonArray ivArr
                ? ivArr.Select(i => new InterviewRecord
                {
                    Id = i.AsBsonDocument.GetValue("id", 0).AsInt32,
                    Date = i.AsBsonDocument.GetValue("date", "").AsString,
                    Type = i.AsBsonDocument.GetValue("type", "Phone").AsString,
                    Notes = i.AsBsonDocument.GetValue("notes", "").AsString,
                    Outcome = i.AsBsonDocument.GetValue("outcome", "Pending").AsString,
                }).ToList()
                : [],
            Salary = doc.TryGetValue("salary", out var salVal) && salVal is BsonDocument salDoc
                ? new SalaryInfo
                {
                    Advertised = salDoc.GetValue("advertised", "").AsString,
                    Target = salDoc.GetValue("target", "").AsString,
                    Discussed = salDoc.GetValue("discussed", "").AsString,
                    Offered = salDoc.GetValue("offered", "").AsString,
                }
                : new SalaryInfo(),
            Recruiter = doc.TryGetValue("recruiter", out var recVal) && recVal is BsonDocument recDoc
                ? new RecruiterInfo
                {
                    Name = recDoc.GetValue("name", "").AsString,
                    Email = recDoc.GetValue("email", "").AsString,
                    LinkedIn = recDoc.GetValue("linkedin", "").AsString,
                }
                : new RecruiterInfo(),
            FollowUpDate = doc.TryGetValue("followUpDate", out var fudVal) && fudVal != BsonNull.Value
                ? fudVal.ToUniversalTime() : null,
            Source = doc.GetValue("source", "").AsString,
            IsHistorical = doc.TryGetValue("isHistorical", out var ihVal) && ihVal.IsBoolean && ihVal.AsBoolean,
        };
    }
}
