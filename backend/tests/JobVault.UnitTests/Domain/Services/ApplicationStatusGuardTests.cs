using JobVault.Domain.Services;

namespace JobVault.UnitTests.Domain.Services;

public class ApplicationStatusGuardTests
{
    [Theory]
    [InlineData("Processing")]
    [InlineData("Queued")]
    [InlineData("Failed")]
    public void Resolve_PipelineOnlyStatuses_ReturnsUpdateInPlace(string status)
    {
        ApplicationStatusGuard.Resolve(status).Should().Be(IngestionMatchAction.UpdateInPlace);
    }

    [Theory]
    [InlineData("Ready to Apply")]
    [InlineData("Regenerating")]
    [InlineData("Researching")]
    public void Resolve_ActivelyEngagedStatuses_ReturnsNoOp(string status)
    {
        ApplicationStatusGuard.Resolve(status).Should().Be(IngestionMatchAction.NoOp);
    }

    [Theory]
    [InlineData("Applied")]
    [InlineData("Rejected")]
    [InlineData("Interview")]
    [InlineData("Offer")]
    [InlineData("Not Interested")]
    [InlineData("Archived")]
    public void Resolve_HumanDecisionStatuses_ReturnsCreateNew(string status)
    {
        ApplicationStatusGuard.Resolve(status).Should().Be(IngestionMatchAction.CreateNew);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SomeUnrecognizedStatus")]
    public void Resolve_UnrecognizedOrMissingStatus_DefaultsToCreateNew(string? status)
    {
        ApplicationStatusGuard.Resolve(status).Should().Be(IngestionMatchAction.CreateNew);
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        ApplicationStatusGuard.Resolve("processing").Should().Be(IngestionMatchAction.UpdateInPlace);
        ApplicationStatusGuard.Resolve("READY TO APPLY").Should().Be(IngestionMatchAction.NoOp);
    }
}
