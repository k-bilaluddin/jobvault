using JobVault.Infrastructure.Vault;

namespace JobVault.UnitTests.Infrastructure.Vault;

public class VaultPathBuilderTests
{
    [Fact]
    public void BuildFolderName_CombinesCompanyNameAndFullId()
    {
        var result = VaultPathBuilder.BuildFolderName("Talon.One", "6a36c73e9b4204b98528c339");

        result.Should().Be("Talon.One-6a36c73e9b4204b98528c339");
    }

    [Fact]
    public void BuildFolderName_DoesNotTruncateId()
    {
        var id = "6a36c73e9b4204b98528c339";

        var result = VaultPathBuilder.BuildFolderName("Acme", id);

        result.Should().Contain(id);
    }

    // ─── Path-unsafe company names ──────────────────────────────────
    // A '/' (or other separator) in the company name gets committed via the GitHub Git Trees
    // API as a literal path component, silently splitting one folder into a nested hierarchy —
    // and, if a component then ends in a space, Windows refuses to check it out at all.

    [Fact]
    public void BuildFolderName_StripsForwardSlash_SoItCannotBecomeANestedPath()
    {
        var result = VaultPathBuilder.BuildFolderName(
            "Confidential AI Startup (via Workfully / jabran farhat)", "6a90b61ea600a27ee5a3eefd");

        result.Should().Be("Confidential AI Startup (via Workfully jabran farhat)-6a90b61ea600a27ee5a3eefd");
        result.Should().NotContain("/");
    }

    [Theory]
    [InlineData("Acme\\Corp", "Acme Corp")]
    [InlineData("Acme:Corp", "Acme Corp")]
    [InlineData("Acme*Corp", "Acme Corp")]
    [InlineData("Acme?Corp", "Acme Corp")]
    [InlineData("Acme\"Corp\"", "Acme Corp")]
    [InlineData("Acme<Corp>", "Acme Corp")]
    [InlineData("Acme|Corp", "Acme Corp")]
    public void BuildFolderName_StripsWindowsInvalidCharacters(string companyName, string expectedPrefix)
    {
        var result = VaultPathBuilder.BuildFolderName(companyName, "id1");

        result.Should().Be($"{expectedPrefix}-id1");
    }

    [Fact]
    public void BuildFolderName_TrimsTrailingSpaceLeftBySanitization_WindowsRejectsTrailingSpace()
    {
        var result = VaultPathBuilder.BuildFolderName("Acme / Corp", "id1");

        result.Should().Be("Acme Corp-id1");
    }
}
