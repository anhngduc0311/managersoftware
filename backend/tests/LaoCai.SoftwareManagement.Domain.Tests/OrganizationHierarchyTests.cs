using FluentAssertions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Application.Services;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
using LaoCai.SoftwareManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LaoCai.SoftwareManagement.Domain.Tests;

public class MockDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
}

public class MockCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public string? UserName { get; set; } = "admin";
    public string? CorrelationId { get; set; } = "test-corr-id";
    public bool IsAuthenticated { get; set; } = true;
}

public class OrganizationHierarchyTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly MockCurrentUserService _currentUserService = new();

    public OrganizationHierarchyTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateDbContext(IDateTimeProvider dtProvider)
    {
        return new AppDbContext(_dbOptions, _currentUserService, dtProvider);
    }

    [Fact]
    public async Task HasCycleAsync_WhenSettingSelfAsParent_ShouldReturnTrue()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var orgService = new OrganizationService(context, dtProvider);

        var orgId = Guid.NewGuid();
        var result = await orgService.HasCycleAsync(orgId, orgId, dtProvider.UtcNow);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCycleAsync_WhenSettingProposedParentThatLeadsToCycle_ShouldReturnTrue()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var orgService = new OrganizationService(context, dtProvider);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var orgC = Guid.NewGuid();

        context.Organizations.AddRange(
            new Organization { Id = orgA, Code = "ORG_A" },
            new Organization { Id = orgB, Code = "ORG_B" },
            new Organization { Id = orgC, Code = "ORG_C" }
        );

        context.OrganizationVersions.AddRange(
            new OrganizationVersion { Id = Guid.NewGuid(), OrganizationId = orgA, Name = "Org A", ParentId = null, ValidFrom = dtProvider.UtcNow.AddYears(-1) },
            new OrganizationVersion { Id = Guid.NewGuid(), OrganizationId = orgB, Name = "Org B", ParentId = orgA, ValidFrom = dtProvider.UtcNow.AddYears(-1) },
            new OrganizationVersion { Id = Guid.NewGuid(), OrganizationId = orgC, Name = "Org C", ParentId = orgB, ValidFrom = dtProvider.UtcNow.AddYears(-1) }
        );
        await context.SaveChangesAsync();

        var result = await orgService.HasCycleAsync(orgA, orgC, dtProvider.UtcNow);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCycleAsync_WhenValidParent_ShouldReturnFalse()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var orgService = new OrganizationService(context, dtProvider);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        context.Organizations.AddRange(
            new Organization { Id = orgA, Code = "ORG_A" },
            new Organization { Id = orgB, Code = "ORG_B" }
        );

        context.OrganizationVersions.Add(
            new OrganizationVersion { Id = Guid.NewGuid(), OrganizationId = orgA, Name = "Org A", ParentId = null, ValidFrom = dtProvider.UtcNow.AddYears(-1) }
        );
        await context.SaveChangesAsync();

        var result = await orgService.HasCycleAsync(orgB, orgA, dtProvider.UtcNow);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasDateOverlapAsync_WhenOverlappingIntervals_ShouldReturnTrue()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var orgService = new OrganizationService(context, dtProvider);

        var orgId = Guid.NewGuid();
        context.Organizations.Add(new Organization { Id = orgId, Code = "ORG_1" });
        context.OrganizationVersions.Add(new OrganizationVersion
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "Version 1",
            ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ValidTo = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var overlap = await orgService.HasDateOverlapAsync(
            orgId,
            new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        overlap.Should().BeTrue();
    }

    [Fact]
    public async Task HasDateOverlapAsync_WhenAdjacentNonOverlappingIntervals_ShouldReturnFalse()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var orgService = new OrganizationService(context, dtProvider);

        var orgId = Guid.NewGuid();
        context.Organizations.Add(new Organization { Id = orgId, Code = "ORG_1" });
        context.OrganizationVersions.Add(new OrganizationVersion
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "Version 1",
            ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ValidTo = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var overlap = await orgService.HasDateOverlapAsync(
            orgId,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        overlap.Should().BeFalse();
    }

    [Fact]
    public async Task GetDescendantOrgIdsAsync_ShouldReturnAllSubTreeChildren()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var orgService = new OrganizationService(context, dtProvider);

        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var orgC = Guid.NewGuid();
        var orgD = Guid.NewGuid();

        context.Organizations.AddRange(
            new Organization { Id = orgA, Code = "ORG_A" },
            new Organization { Id = orgB, Code = "ORG_B" },
            new Organization { Id = orgC, Code = "ORG_C" },
            new Organization { Id = orgD, Code = "ORG_D" }
        );

        context.OrganizationVersions.AddRange(
            new OrganizationVersion { Id = Guid.NewGuid(), OrganizationId = orgA, Name = "Org A", ParentId = null, ValidFrom = dtProvider.UtcNow.AddYears(-1) },
            new OrganizationVersion { Id = Guid.NewGuid(), OrganizationId = orgB, Name = "Org B", ParentId = orgA, ValidFrom = dtProvider.UtcNow.AddYears(-1) },
            new OrganizationVersion { Id = Guid.NewGuid(), OrganizationId = orgC, Name = "Org C", ParentId = orgB, ValidFrom = dtProvider.UtcNow.AddYears(-1) },
            new OrganizationVersion { Id = Guid.NewGuid(), OrganizationId = orgD, Name = "Org D", ParentId = null, ValidFrom = dtProvider.UtcNow.AddYears(-1) }
        );
        await context.SaveChangesAsync();

        var descendants = await orgService.GetDescendantOrgIdsAsync(orgA, dtProvider.UtcNow);
        descendants.Should().Contain(new[] { orgB, orgC });
        descendants.Should().NotContain(orgD);
    }
}
