using LaoCai.SoftwareManagement.Domain.Entities.Audit;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;

namespace LaoCai.SoftwareManagement.Domain.Tests;

public class UserRoleScopeTests
{
    [Fact]
    public void IsCurrentlyValid_ShouldReturnTrue_WhenCurrentTimeWithinRange()
    {
        // Arrange
        var now = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);
        var scope = new UserRoleScope
        {
            ValidFrom = now.AddDays(-1),
            ValidTo = now.AddDays(1)
        };

        // Act & Assert
        scope.IsCurrentlyValid(now).Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyValid_ShouldReturnTrue_WhenValidToIsNull()
    {
        // Arrange
        var now = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);
        var scope = new UserRoleScope
        {
            ValidFrom = now.AddDays(-10),
            ValidTo = null
        };

        // Act & Assert
        scope.IsCurrentlyValid(now).Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyValid_ShouldReturnFalse_WhenCurrentTimeIsPastValidTo()
    {
        // Arrange
        var now = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);
        var scope = new UserRoleScope
        {
            ValidFrom = now.AddDays(-10),
            ValidTo = now.AddDays(-1)
        };

        // Act & Assert
        scope.IsCurrentlyValid(now).Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyValid_ShouldReturnFalse_WhenCurrentTimeIsBeforeValidFrom()
    {
        // Arrange
        var now = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);
        var scope = new UserRoleScope
        {
            ValidFrom = now.AddDays(1),
            ValidTo = now.AddDays(10)
        };

        // Act & Assert
        scope.IsCurrentlyValid(now).Should().BeFalse();
    }
}

public class AuditLogTests
{
    [Fact]
    public void Create_ShouldPopulateAllFieldsCorrectly()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var entityId = Guid.NewGuid().ToString();

        // Act
        var audit = AuditLog.Create(
            actorId,
            "Create",
            "Deployment",
            entityId,
            orgId,
            null,
            "{\"Name\":\"Portal\"}",
            "corr-123");

        // Assert
        audit.Id.Should().NotBeEmpty();
        audit.ActorId.Should().Be(actorId);
        audit.Action.Should().Be("Create");
        audit.EntityType.Should().Be("Deployment");
        audit.EntityId.Should().Be(entityId);
        audit.OrganizationId.Should().Be(orgId);
        audit.AfterJson.Should().Be("{\"Name\":\"Portal\"}");
        audit.CorrelationId.Should().Be("corr-123");
        audit.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
