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
    private readonly IMongoCollection<JobApplicationDocument> _collection;
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
        _collection = database.GetCollection<JobApplicationDocument>(collectionName);

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

            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.CompanyName, application.CompanyName);
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
                application.CreatedAt = existing!.CreatedAt ?? DateTime.UtcNow;
                id = existing.Id;
            }

            var doc = JobApplicationMapper.ToDocument(application, id);

            if (isNew)
            {
                await _collection.InsertOneAsync(doc);
                _logger.LogInformation("Inserted new job application for {CompanyName} (id={Id})", application.CompanyName, id);
            }
            else
            {
                // Preserve existing tracker fields on re-ingestion
                doc.Stage = (existing!.Stage?.Length > 0) ? existing.Stage : "Ready to Apply";
                doc.Applied = existing.Applied ?? false;
                doc.AppliedDate = existing.AppliedDate;
                doc.PersonalNotes = existing.PersonalNotes ?? "";
                doc.Interviews = existing.Interviews ?? [];
                doc.Salary = existing.Salary ?? new SalaryDocument();
                doc.Recruiter = existing.Recruiter ?? new RecruiterDocument();
                doc.FollowUpDate = existing.FollowUpDate;
                doc.Source = (existing.Source?.Length > 0) ? existing.Source : doc.Source;
                doc.IsHistorical = existing.IsHistorical ?? false;

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
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            return doc == null ? null : JobApplicationMapper.ToDomain(doc);
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
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);

            var updateDef = Builders<JobApplicationDocument>.Update
                .Set(d => d.Status, status)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            if (commitUrl != null)
                updateDef = updateDef.Set(d => d.CommitUrl, commitUrl);

            if (errorDetails != null)
                updateDef = updateDef.Set(d => d.ErrorDetails, errorDetails);

            // Clear generation payload once processing is done
            // Keep compatibilityReportMarkdown and tailoringNotesMarkdown for API serving
            if (status is "Ready to Apply" or "Failed")
            {
                updateDef = updateDef
                    .Unset(d => d.JdSource)
                    .Unset(d => d.Headline)
                    .Unset(d => d.Summary)
                    .Unset(d => d.Skills)
                    .Unset(d => d.Roles)
                    .Unset(d => d.Recipient)
                    .Unset(d => d.CoverLetterParagraphs)
                    .Unset(d => d.TailoringNotes);
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
            var docs = await _collection
                .Find(FilterDefinition<JobApplicationDocument>.Empty)
                .Sort(Builders<JobApplicationDocument>.Sort.Descending(d => d.UpdatedAt))
                .ToListAsync(cancellationToken);

            return docs.Select(JobApplicationMapper.ToDomain).ToList();
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
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.CompanyName, companyName);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            return doc == null ? null : JobApplicationMapper.ToDomain(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching job application by companyName={CompanyName}", companyName);
            return null;
        }
    }

    public async Task<bool> UpdateStageAsync(string companyName, string stage, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.CompanyName, companyName);

            var update = Builders<JobApplicationDocument>.Update
                .Set(d => d.Stage, stage)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            if (stage == "Applied")
            {
                update = update
                    .Set(d => d.Applied, true)
                    .Set(d => d.AppliedDate, DateTime.UtcNow);
            }

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.MatchedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stage for {CompanyName}", companyName);
            return false;
        }
    }

    public async Task<bool> UpdatePersonalNotesAsync(string companyName, string notes, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.CompanyName, companyName);

            var update = Builders<JobApplicationDocument>.Update
                .Set(d => d.PersonalNotes, notes)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.MatchedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating personal notes for {CompanyName}", companyName);
            return false;
        }
    }

    public async Task<JobApplication?> AddInterviewAsync(string companyName, Domain.ValueObjects.InterviewRecord interview, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.CompanyName, companyName);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (doc == null) return null;

            var interviews = doc.Interviews ?? [];
            interview.Id = interviews.Count;

            var interviewDoc = new InterviewDocument
            {
                Id = interview.Id,
                Date = interview.Date,
                Type = interview.Type,
                Notes = interview.Notes,
                Outcome = interview.Outcome,
            };

            var update = Builders<JobApplicationDocument>.Update
                .Push(d => d.Interviews, interviewDoc)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            doc.Interviews!.Add(interviewDoc);
            return JobApplicationMapper.ToDomain(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding interview for {CompanyName}", companyName);
            return null;
        }
    }

    public async Task<bool> DeleteInterviewAsync(string companyName, int index, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.CompanyName, companyName);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (doc == null) return false;

            var interviews = doc.Interviews ?? [];
            if (index < 0 || index >= interviews.Count) return false;

            interviews.RemoveAt(index);
            for (var i = 0; i < interviews.Count; i++)
                interviews[i].Id = i;

            var update = Builders<JobApplicationDocument>.Update
                .Set(d => d.Interviews, interviews)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.MatchedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting interview for {CompanyName}", companyName);
            return false;
        }
    }
}
