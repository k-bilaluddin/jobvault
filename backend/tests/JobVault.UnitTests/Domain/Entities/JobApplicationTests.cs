using JobVault.Domain.Entities;
using JobVault.Domain.ValueObjects;

namespace JobVault.UnitTests.Domain.Entities;

public class JobApplicationTests
{
    [Fact]
    public void NewJobApplication_StringPropertiesDefaultToEmpty()
    {
        // Arrange & Act
        var app = new JobApplication();

        // Assert
        app.CompanyName.Should().BeEmpty();
        app.JobTitle.Should().BeEmpty();
        app.Location.Should().BeEmpty();
        app.JobUrl.Should().BeEmpty();
        app.WorkMode.Should().BeEmpty();
        app.EmploymentType.Should().BeEmpty();
        app.Currency.Should().BeEmpty();
        app.SalaryPeriod.Should().BeEmpty();
        app.Recommendation.Should().BeEmpty();
        app.Status.Should().BeEmpty();
    }

    [Fact]
    public void NewJobApplication_NullablePropertiesDefaultToNull()
    {
        // Arrange & Act
        var app = new JobApplication();

        // Assert
        app.Id.Should().BeNull();
        app.JdSource.Should().BeNull();
        app.Headline.Should().BeNull();
        app.Summary.Should().BeNull();
        app.Recipient.Should().BeNull();
        app.TailoringNotes.Should().BeNull();
        app.CommitUrl.Should().BeNull();
        app.ErrorDetails.Should().BeNull();
        app.CompatibilityReportMarkdown.Should().BeNull();
        app.TailoringNotesMarkdown.Should().BeNull();
    }

    [Fact]
    public void NewJobApplication_CollectionPropertiesDefaultToEmptyLists()
    {
        // Arrange & Act
        var app = new JobApplication();

        // Assert
        app.Skills.Should().NotBeNull().And.HaveCount(0);
        app.Roles.Should().NotBeNull().And.HaveCount(0);
        app.CoverLetterParagraphs.Should().NotBeNull().And.HaveCount(0);
        app.Strengths.Should().NotBeNull().And.HaveCount(0);
        app.Gaps.Should().NotBeNull().And.HaveCount(0);
    }

    [Fact]
    public void NewJobApplication_MatchScoreDefaultsToZero()
    {
        // Arrange & Act
        var app = new JobApplication();

        // Assert
        app.MatchScore.Should().Be(0);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var app = new JobApplication
        {
            Id = "abc123",
            CompanyName = "Acme Corp",
            JobTitle = "Senior Engineer",
            Location = "Remote",
            JobUrl = "https://example.com/job",
            WorkMode = "Remote",
            EmploymentType = "Full-time",
            SalaryMin = 100000,
            SalaryMax = 150000,
            Currency = "USD",
            SalaryPeriod = "Annual",
            MatchScore = 92,
            Recommendation = "Strong match",
            Status = "Processing",
            CreatedAt = now,
            UpdatedAt = now,
            JdSource = "LinkedIn",
            Headline = "Great opportunity",
            Summary = "A summary",
            Recipient = "Hiring Manager",
            TailoringNotes = "Focus on leadership",
            CommitUrl = "https://github.com/commit/abc",
            ErrorDetails = null,
            CompatibilityReportMarkdown = "## Report",
            TailoringNotesMarkdown = "## Notes",
            Skills = new List<SkillRow> { new() { Label = "C#", Value = "Expert" } },
            Roles = new List<RolePayload> { new() { Id = "senior_dev" } },
            CoverLetterParagraphs = new List<string> { "Dear Hiring Manager" },
            Strengths = new List<string> { "Leadership" },
            Gaps = new List<string> { "Go experience" }
        };

        // Act & Assert
        app.Id.Should().Be("abc123");
        app.CompanyName.Should().Be("Acme Corp");
        app.JobTitle.Should().Be("Senior Engineer");
        app.Location.Should().Be("Remote");
        app.JobUrl.Should().Be("https://example.com/job");
        app.WorkMode.Should().Be("Remote");
        app.EmploymentType.Should().Be("Full-time");
        app.SalaryMin.Should().Be(100000);
        app.SalaryMax.Should().Be(150000);
        app.Currency.Should().Be("USD");
        app.SalaryPeriod.Should().Be("Annual");
        app.MatchScore.Should().Be(92);
        app.Recommendation.Should().Be("Strong match");
        app.Status.Should().Be("Processing");
        app.CreatedAt.Should().Be(now);
        app.UpdatedAt.Should().Be(now);
        app.JdSource.Should().Be("LinkedIn");
        app.Headline.Should().Be("Great opportunity");
        app.Summary.Should().Be("A summary");
        app.Recipient.Should().Be("Hiring Manager");
        app.TailoringNotes.Should().Be("Focus on leadership");
        app.CommitUrl.Should().Be("https://github.com/commit/abc");
        app.ErrorDetails.Should().BeNull();
        app.CompatibilityReportMarkdown.Should().Be("## Report");
        app.TailoringNotesMarkdown.Should().Be("## Notes");
        app.Skills.Should().HaveCount(1);
        app.Roles.Should().HaveCount(1);
        app.CoverLetterParagraphs.Should().HaveCount(1);
        app.Strengths.Should().HaveCount(1);
        app.Gaps.Should().HaveCount(1);
    }

    [Fact]
    public void CollectionProperties_CanAddItems()
    {
        // Arrange
        var app = new JobApplication();

        // Act
        app.Skills.Add(new SkillRow { Label = "C#", Value = "Expert" });
        app.Skills.Add(new SkillRow { Label = "TypeScript", Value = "Advanced" });
        app.Roles.Add(new RolePayload { Id = "calvergy" });
        app.CoverLetterParagraphs.Add("First paragraph");
        app.Strengths.Add("Problem solving");
        app.Gaps.Add("Machine learning");

        // Assert
        app.Skills.Should().HaveCount(2);
        app.Roles.Should().HaveCount(1);
        app.CoverLetterParagraphs.Should().HaveCount(1);
        app.Strengths.Should().HaveCount(1);
        app.Gaps.Should().HaveCount(1);
    }
}
