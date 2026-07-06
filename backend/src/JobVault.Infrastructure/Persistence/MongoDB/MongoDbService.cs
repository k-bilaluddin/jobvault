using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using JobVault.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace JobVault.Infrastructure.Persistence.MongoDB;

public class MongoDbService : IJobApplicationRepository
{
    private readonly IMongoCollection<JobApplicationDocument> _collection;
    private readonly ILogger<MongoDbService> _logger;

    // Closes the TOCTOU window in UpsertApplicationAsync between FindMatchAsync and the
    // insert/update decision: two concurrent ingestions resolving to the same identity would
    // otherwise both miss the match and both insert. Scoped to this process (the only caller,
    // ApplicationIngestionService, only ever runs in the API process) — a DB-level unique
    // constraint was considered instead, but a partial unique index on (companyName, jobTitle)
    // for in-flight statuses would also fire on ReQueueAsync's UpdateStatusAsync("Queued")
    // transition whenever a second in-flight document for the same company+title legitimately
    // coexists (which the Tier 3 "create new" design explicitly allows), breaking Re-Analyze.
    // Entries are never evicted; acceptable at this application's scale (one process, a few
    // hundred applications over its lifetime).
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _ingestionLocks = new();

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

        EnsureIndexes();

        _logger.LogInformation("MongoDbService initialized with database: {Database}, collection: {Collection}",
            databaseName, collectionName);
    }

    private void EnsureIndexes()
    {
        var jobUrlNormalizedIndex = new CreateIndexModel<JobApplicationDocument>(
            Builders<JobApplicationDocument>.IndexKeys.Ascending(d => d.JobUrlNormalized),
            new CreateIndexOptions { Sparse = true });

        _collection.Indexes.CreateMany([jobUrlNormalizedIndex]);
    }

    public async Task<UpsertResult> UpsertApplicationAsync(JobApplication application)
    {
        if (string.IsNullOrWhiteSpace(application.CompanyName))
        {
            _logger.LogWarning("Cannot upsert application with empty CompanyName");
            return UpsertResult.Failure();
        }

        var lockKey = BuildIngestionLockKey(application);
        var gate = _ingestionLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync();
        try
        {
            var existing = await FindMatchAsync(application);
            if (existing == null)
                return await InsertNewAsync(application);

            return ApplicationStatusGuard.Resolve(existing.Status) switch
            {
                IngestionMatchAction.UpdateInPlace => await ReplaceExistingAsync(existing, application),
                IngestionMatchAction.NoOp => UpsertResult.NoOp(existing.Id),
                _ => await InsertNewAsync(application),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting job application for {CompanyName}", application.CompanyName);
            return UpsertResult.Failure();
        }
        finally
        {
            gate.Release();
        }
    }

    // Always keyed by (companyName, jobTitle), never by URL alone: FindMatchAsync falls through
    // to the company+title fallback whenever the URL doesn't match an existing document, so two
    // concurrent requests with *different* URLs for what's meant to be the same role (the repost
    // race) must still serialize against each other, not just requests sharing one exact URL.
    private static string BuildIngestionLockKey(JobApplication application) =>
        $"{application.CompanyName.Trim().ToLowerInvariant()}|{application.JobTitle.Trim().ToLowerInvariant()}";

    public async Task<UpsertResult> UpdateApplicationByIdAsync(string applicationId, JobApplication application, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, applicationId);
            var existing = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (existing == null)
            {
                _logger.LogWarning("UpdateApplicationByIdAsync: application not found: {Id}", applicationId);
                return UpsertResult.Failure();
            }

            return await ReplaceExistingAsync(existing, application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job application by id={Id}", applicationId);
            return UpsertResult.Failure();
        }
    }

    /// <summary>
    /// Tier 1 (normalized URL) then Tier 2 (company+title fallback) of the identity resolution
    /// cascade. Tier 0 (PendingJob.SourceApplicationId) is resolved by the caller before this is
    /// ever reached — see ApplicationIngestionService.
    /// </summary>
    private async Task<JobApplicationDocument?> FindMatchAsync(JobApplication application)
    {
        if (!string.IsNullOrWhiteSpace(application.JobUrlNormalized))
        {
            var urlFilter = Builders<JobApplicationDocument>.Filter.Eq(d => d.JobUrlNormalized, application.JobUrlNormalized);
            var urlMatch = await _collection.Find(urlFilter).FirstOrDefaultAsync();
            if (urlMatch != null)
                return urlMatch;
        }

        var companyPattern = new BsonRegularExpression($"^{Regex.Escape(application.CompanyName.Trim())}$", "i");
        var titlePattern = new BsonRegularExpression($"^{Regex.Escape(application.JobTitle.Trim())}$", "i");

        var fallbackFilter = Builders<JobApplicationDocument>.Filter.And(
            Builders<JobApplicationDocument>.Filter.Regex(d => d.CompanyName, companyPattern),
            Builders<JobApplicationDocument>.Filter.Regex(d => d.JobTitle, titlePattern));

        return await _collection.Find(fallbackFilter).FirstOrDefaultAsync();
    }

    private async Task<UpsertResult> InsertNewAsync(JobApplication application)
    {
        var id = ObjectId.GenerateNewId().ToString();
        application.CreatedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        var doc = JobApplicationMapper.ToDocument(application, id);
        await _collection.InsertOneAsync(doc);

        _logger.LogInformation("Inserted new job application for {CompanyName} (id={Id})", application.CompanyName, id);
        return UpsertResult.Success(isNewDocument: true, id);
    }

    private async Task<UpsertResult> ReplaceExistingAsync(JobApplicationDocument existing, JobApplication application)
    {
        application.UpdatedAt = DateTime.UtcNow;
        application.CreatedAt = existing.CreatedAt ?? DateTime.UtcNow;

        var doc = JobApplicationMapper.ToDocument(application, existing.Id);

        // Preserve existing tracker fields on re-ingestion
        doc.Stage = (existing.Stage?.Length > 0) ? existing.Stage : "Ready to Apply";
        doc.Applied = existing.Applied ?? false;
        doc.AppliedDate = existing.AppliedDate;
        doc.PersonalNotes = existing.PersonalNotes ?? "";
        doc.Interviews = existing.Interviews ?? [];
        doc.Notes = existing.Notes ?? [];
        doc.Salary = existing.Salary ?? new SalaryDocument();
        doc.Recruiter = existing.Recruiter ?? new RecruiterDocument();
        doc.FollowUpDate = existing.FollowUpDate;
        doc.Source = (existing.Source?.Length > 0) ? existing.Source : doc.Source;
        doc.IsHistorical = existing.IsHistorical ?? false;

        var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, existing.Id);
        await _collection.ReplaceOneAsync(filter, doc);

        _logger.LogInformation("Updated existing job application for {CompanyName} (id={Id})", application.CompanyName, existing.Id);
        return UpsertResult.Success(isNewDocument: false, existing.Id);
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

    public async Task<ApplicationPageResult> GetPagedApplicationsAsync(
        int page,
        int pageSize,
        string? search = null,
        string? stage = null,
        string sortBy = "synced_at",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var builder = Builders<JobApplicationDocument>.Filter;
            var filter = builder.Ne(d => d.IsHistorical, true);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var regex = new BsonRegularExpression(search, "i");
                filter &= builder.Or(
                    builder.Regex(d => d.CompanyName, regex),
                    builder.Regex(d => d.JobTitle, regex));
            }

            if (!string.IsNullOrWhiteSpace(stage) && stage != "All")
            {
                var statusStages = new[] { "Processing", "Failed", "Regenerating", "Queued" };
                if (statusStages.Contains(stage))
                {
                    filter &= builder.Eq(d => d.Status, stage);
                }
                else if (stage == "Ready to Apply")
                {
                    filter &= builder.Or(
                        builder.Eq(d => d.Stage, stage),
                        builder.Eq(d => d.Stage, ""),
                        builder.Eq(d => d.Stage, null));
                    filter &= builder.Nin(d => d.Status, statusStages);
                }
                else
                {
                    filter &= builder.Eq(d => d.Stage, stage);
                    filter &= builder.Nin(d => d.Status, statusStages);
                }
            }

            // Stage counts — run on non-historical filter only (before stage filter)
            var baseFilter = builder.Ne(d => d.IsHistorical, true);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var regex = new BsonRegularExpression(search, "i");
                baseFilter &= builder.Or(
                    builder.Regex(d => d.CompanyName, regex),
                    builder.Regex(d => d.JobTitle, regex));
            }

            var allDocs = await _collection.Find(baseFilter)
                .Project(Builders<JobApplicationDocument>.Projection.Include(d => d.Stage).Include(d => d.Status))
                .ToListAsync(cancellationToken);

            var stageCounts = new Dictionary<string, int>();
            var totalAll = 0;
            foreach (var doc in allDocs)
            {
                totalAll++;
                var status = doc.GetValue("status", "").AsString;
                var docStage = doc.GetValue("stage", "").AsString;
                var effectiveStage = status is "Processing" or "Failed" or "Regenerating" or "Queued"
                    ? status
                    : string.IsNullOrEmpty(docStage) ? "Ready to Apply" : docStage;
                stageCounts[effectiveStage] = stageCounts.GetValueOrDefault(effectiveStage) + 1;
            }
            stageCounts["All"] = totalAll;

            var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

            var sort = BuildSort(sortBy, sortDirection);

            var docs2 = await _collection
                .Find(filter)
                .Sort(sort)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            return new ApplicationPageResult(
                docs2.Select(JobApplicationMapper.ToDomain).ToList(),
                (int)totalCount,
                stageCounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paged job applications");
            return new ApplicationPageResult([], 0, new Dictionary<string, int>());
        }
    }

    private static SortDefinition<JobApplicationDocument> BuildSort(string sortBy, string direction)
    {
        var isAsc = direction.Equals("asc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "name" => isAsc
                ? Builders<JobApplicationDocument>.Sort.Ascending(d => d.CompanyName)
                : Builders<JobApplicationDocument>.Sort.Descending(d => d.CompanyName),
            "match_pct" => isAsc
                ? Builders<JobApplicationDocument>.Sort.Ascending(d => d.MatchScore)
                : Builders<JobApplicationDocument>.Sort.Descending(d => d.MatchScore),
            "applied_date" => isAsc
                ? Builders<JobApplicationDocument>.Sort.Ascending(d => d.AppliedDate)
                : Builders<JobApplicationDocument>.Sort.Descending(d => d.AppliedDate),
            "stage" => isAsc
                ? Builders<JobApplicationDocument>.Sort.Ascending(d => d.Stage)
                : Builders<JobApplicationDocument>.Sort.Descending(d => d.Stage),
            "salary" => isAsc
                ? Builders<JobApplicationDocument>.Sort.Ascending("salary.advertised")
                : Builders<JobApplicationDocument>.Sort.Descending("salary.advertised"),
            _ => isAsc
                ? Builders<JobApplicationDocument>.Sort.Ascending(d => d.UpdatedAt)
                : Builders<JobApplicationDocument>.Sort.Descending(d => d.UpdatedAt),
        };
    }

    public async Task<bool> UpdateStageAsync(string id, string stage, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);

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
            _logger.LogError(ex, "Error updating stage for id={Id}", id);
            return false;
        }
    }

    public async Task<bool> UpdatePersonalNotesAsync(string id, string notes, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);

            var update = Builders<JobApplicationDocument>.Update
                .Set(d => d.PersonalNotes, notes)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.MatchedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating personal notes for id={Id}", id);
            return false;
        }
    }

    public async Task<JobApplication?> AddInterviewAsync(string id, Domain.ValueObjects.InterviewRecord interview, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);
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
            _logger.LogError(ex, "Error adding interview for id={Id}", id);
            return null;
        }
    }

    public async Task<JobApplication?> UpdateInterviewAsync(string id, int index, string? date, string? type, string? notes, string? outcome, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (doc == null) return null;

            var interviews = doc.Interviews ?? [];
            if (index < 0 || index >= interviews.Count) return null;

            var iv = interviews[index];
            if (date != null) iv.Date = date;
            if (type != null) iv.Type = type;
            if (notes != null) iv.Notes = notes;
            if (outcome != null) iv.Outcome = outcome;

            var update = Builders<JobApplicationDocument>.Update
                .Set(d => d.Interviews, interviews)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return JobApplicationMapper.ToDomain(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating interview for id={Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteInterviewAsync(string id, int index, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);
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
            _logger.LogError(ex, "Error deleting interview for id={Id}", id);
            return false;
        }
    }

    public async Task<JobApplication?> AddNoteAsync(string id, Domain.ValueObjects.ApplicationNote note, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (doc == null) return null;

            var notes = doc.Notes ?? [];
            note.Id = notes.Count;

            var noteDoc = new NoteDocument
            {
                Id = note.Id,
                Category = note.Category,
                Content = note.Content,
                Stage = note.Stage,
                Pinned = note.Pinned,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt,
            };

            var update = Builders<JobApplicationDocument>.Update
                .Push(d => d.Notes, noteDoc)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            doc.Notes ??= [];
            doc.Notes.Add(noteDoc);
            return JobApplicationMapper.ToDomain(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding note for id={Id}", id);
            return null;
        }
    }

    public async Task<JobApplication?> UpdateNoteAsync(string id, int noteId, string? category, string? content, bool? pinned, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (doc == null) return null;

            var notes = doc.Notes ?? [];
            var note = notes.FirstOrDefault(n => n.Id == noteId);
            if (note == null) return null;

            if (category != null) note.Category = category;
            if (content != null) note.Content = content;
            if (pinned.HasValue) note.Pinned = pinned.Value;
            note.UpdatedAt = DateTime.UtcNow;

            var update = Builders<JobApplicationDocument>.Update
                .Set(d => d.Notes, notes)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return JobApplicationMapper.ToDomain(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note for id={Id}", id);
            return null;
        }
    }

    public async Task<bool> UpdateContentAsync(
        string id,
        string? headline,
        string? summary,
        List<Domain.ValueObjects.SkillRow>? skills,
        List<Domain.ValueObjects.RolePayload>? roles,
        string? recipient,
        List<string>? coverLetterParagraphs,
        List<string>? strengths,
        List<string>? gaps,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);
            var update = Builders<JobApplicationDocument>.Update.Set(d => d.UpdatedAt, DateTime.UtcNow);

            if (headline != null) update = update.Set(d => d.Headline, headline);
            if (summary != null) update = update.Set(d => d.Summary, summary);
            if (skills != null) update = update.Set(d => d.Skills, skills.Select(s => new SkillRowDocument { Label = s.Label, Value = s.Value }).ToList());
            if (roles != null) update = update.Set(d => d.Roles, roles.Select(r => new RolePayloadDocument { Id = r.Id, Bullets = r.Bullets }).ToList());
            if (recipient != null) update = update.Set(d => d.Recipient, recipient);
            if (coverLetterParagraphs != null) update = update.Set(d => d.CoverLetterParagraphs, coverLetterParagraphs);
            if (strengths != null) update = update.Set(d => d.Strengths, strengths);
            if (gaps != null) update = update.Set(d => d.Gaps, gaps);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.MatchedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating content for id={Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteNoteAsync(string id, int noteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobApplicationDocument>.Filter.Eq(d => d.Id, id);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (doc == null) return false;

            var notes = doc.Notes ?? [];
            var idx = notes.FindIndex(n => n.Id == noteId);
            if (idx < 0) return false;

            notes.RemoveAt(idx);
            for (var i = 0; i < notes.Count; i++)
                notes[i].Id = i;

            var update = Builders<JobApplicationDocument>.Update
                .Set(d => d.Notes, notes)
                .Set(d => d.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.MatchedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note for id={Id}", id);
            return false;
        }
    }
}
