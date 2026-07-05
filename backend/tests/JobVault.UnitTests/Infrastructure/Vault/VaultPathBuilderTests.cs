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
}
