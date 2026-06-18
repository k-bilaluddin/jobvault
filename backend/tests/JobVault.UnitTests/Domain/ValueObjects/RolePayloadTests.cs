using JobVault.Domain.ValueObjects;

namespace JobVault.UnitTests.Domain.ValueObjects;

public class RolePayloadTests
{
    [Fact]
    public void NewRolePayload_IdDefaultsToEmpty()
    {
        // Arrange & Act
        var role = new RolePayload();

        // Assert
        role.Id.Should().BeEmpty();
    }

    [Fact]
    public void NewRolePayload_BulletsDefaultsToEmptyList()
    {
        // Arrange & Act
        var role = new RolePayload();

        // Assert
        role.Bullets.Should().NotBeNull().And.HaveCount(0);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        // Arrange & Act
        var role = new RolePayload
        {
            Id = "calvergy",
            Bullets = new List<string> { "a", "b" }
        };

        // Assert
        role.Id.Should().Be("calvergy");
        role.Bullets.Should().HaveCount(2);
        role.Bullets[0].Should().Be("a");
        role.Bullets[1].Should().Be("b");
    }
}
