using JobVault.Domain.ValueObjects;

namespace JobVault.UnitTests.Domain.ValueObjects;

public class SkillRowTests
{
    [Fact]
    public void NewSkillRow_LabelDefaultsToEmpty()
    {
        // Arrange & Act
        var skill = new SkillRow();

        // Assert
        skill.Label.Should().BeEmpty();
    }

    [Fact]
    public void NewSkillRow_ValueDefaultsToEmpty()
    {
        // Arrange & Act
        var skill = new SkillRow();

        // Assert
        skill.Value.Should().BeEmpty();
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        // Arrange & Act
        var skill = new SkillRow
        {
            Label = "C#",
            Value = "Expert"
        };

        // Assert
        skill.Label.Should().Be("C#");
        skill.Value.Should().Be("Expert");
    }
}
