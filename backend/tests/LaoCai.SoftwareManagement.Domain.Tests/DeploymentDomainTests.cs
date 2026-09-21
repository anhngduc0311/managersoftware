using FluentAssertions;
using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Application.Services;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using LaoCai.SoftwareManagement.Domain.Entities.Deployments;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
using LaoCai.SoftwareManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LaoCai.SoftwareManagement.Domain.Tests;

public class DeploymentDomainTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly MockCurrentUserService _currentUserService = new();

    public DeploymentDomainTests()
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
    public void DeploymentRevision_WhenCreated_DefaultStatusShouldBeDraft()
    {
        var rev = new DeploymentRevision
        {
            Id = Guid.NewGuid(),
            DeploymentId = Guid.NewGuid(),
            RevisionNo = 1
        };

        rev.WorkflowStatus.Should().Be("Draft");
        rev.OperationalStatus.Should().Be("NotInUse");
        rev.Version.Should().Be(1);
    }

    [Fact]
    public async Task Deployment_Version_ShouldIncrementOnUpdate()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);

        var softwareId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var dep = new Deployment
        {
            Id = Guid.NewGuid(),
            SoftwareId = softwareId,
            OrganizationId = orgId,
            Environment = "Production",
            InstanceKey = "default",
            Version = 1
        };

        context.Deployments.Add(dep);
        await context.SaveChangesAsync();

        dep.Version.Should().Be(1);

        dep.InstanceKey = "instance-2";
        await context.SaveChangesAsync();

        dep.Version.Should().Be(2);
    }

    [Fact]
    public void ApprovalDecision_WhenRejected_ReasonShouldBePreserved()
    {
        var actorId = Guid.NewGuid();
        var revId = Guid.NewGuid();

        var decision = new ApprovalDecision
        {
            Id = Guid.NewGuid(),
            DeploymentRevisionId = revId,
            Decision = "Rejected",
            Reason = "Thiếu tài liệu tiếp nhận và ngày đưa vào sử dụng không chính xác",
            ActorId = actorId,
            DecidedAt = DateTime.UtcNow
        };

        decision.Decision.Should().Be("Rejected");
        decision.Reason.Should().NotBeNullOrWhiteSpace();
        decision.ActorId.Should().Be(actorId);
    }
}
