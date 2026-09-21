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

public class MockWorkflowScopeAuthorizationService : IScopeAuthorizationService
{
    public bool AllowAll { get; set; } = true;

    public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, Guid? targetOrgId = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AllowAll);
    }

    public Task<HashSet<Guid>?> GetAllowedOrganizationIdsAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<HashSet<Guid>?>(null);
    }

    public Task<List<UserGrantDto>> GetUserActiveGrantsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<UserGrantDto>());
    }
}

public class DeploymentWorkflowTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly MockCurrentUserService _currentUserService = new();
    private readonly MockDateTimeProvider _dateTimeProvider = new();
    private readonly MockWorkflowScopeAuthorizationService _scopeAuth = new();

    public DeploymentWorkflowTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateDbContext()
    {
        return new AppDbContext(_dbOptions, _currentUserService, _dateTimeProvider);
    }

    private (DeploymentService Service, BackgroundJobService JobService) CreateServices(AppDbContext context)
    {
        var jobService = new BackgroundJobService(context, _dateTimeProvider);
        var deploymentService = new DeploymentService(context, _scopeAuth, jobService, _dateTimeProvider);
        return (deploymentService, jobService);
    }

    private async Task<(Guid SoftwareId, Guid ReleaseId, Guid OrgId, Guid EditorId, Guid ApproverId)> SeedBaseEntitiesAsync(AppDbContext context)
    {
        var orgId = Guid.NewGuid();
        var org = new Organization { Id = orgId, Code = "ORG_TEST", IsActive = true };
        org.Versions.Add(new OrganizationVersion
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "Đơn vị Kiểm thử",
            ValidFrom = DateTime.UtcNow.AddYears(-1)
        });
        context.Organizations.Add(org);

        var cat = new SoftwareCategory { Id = Guid.NewGuid(), Code = "CAT1", Name = "Category 1" };
        var ven = new Vendor { Id = Guid.NewGuid(), Code = "VEN1", Name = "Vendor 1" };
        context.SoftwareCategories.Add(cat);
        context.Vendors.Add(ven);

        var swId = Guid.NewGuid();
        var relId = Guid.NewGuid();
        var sw = new Software
        {
            Id = swId,
            Code = "SW_TEST",
            Name = "Phần mềm Kiểm thử",
            CategoryId = cat.Id,
            VendorId = ven.Id,
            LifecycleStatus = "Active"
        };
        sw.Releases.Add(new SoftwareRelease
        {
            Id = relId,
            SoftwareId = swId,
            VersionName = "v1.0.0",
            ReleaseDate = DateTime.UtcNow.AddMonths(-1)
        });
        context.Software.Add(sw);

        var editorId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = editorId,
            UserName = "editor",
            NormalizedUserName = "EDITOR",
            DisplayName = "Cán bộ nhập",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        context.Users.Add(new User
        {
            Id = approverId,
            UserName = "approver",
            NormalizedUserName = "APPROVER",
            DisplayName = "Lãnh đạo duyệt",
            SecurityStamp = Guid.NewGuid().ToString()
        });

        await context.SaveChangesAsync();
        return (swId, relId, orgId, editorId, approverId);
    }

    [Fact]
    public async Task CreateDeployment_WithValidData_ShouldCreateDeploymentAndDraftRevision1()
    {
        using var context = CreateDbContext();
        var (swId, relId, orgId, editorId, _) = await SeedBaseEntitiesAsync(context);
        var (service, _) = CreateServices(context);

        var createDto = new CreateDeploymentDto(
            swId,
            orgId,
            "Production",
            "default",
            relId,
            "NotInUse",
            null,
            null,
            editorId);

        var result = await service.CreateDeploymentAsync(createDto, editorId);

        result.Should().NotBeNull();
        result.SoftwareId.Should().Be(swId);
        result.OrganizationId.Should().Be(orgId);
        result.CurrentApprovedRevisionId.Should().BeNull();
        result.ActiveRevision.Should().NotBeNull();
        result.ActiveRevision!.RevisionNo.Should().Be(1);
        result.ActiveRevision.WorkflowStatus.Should().Be("Draft");
    }

    [Fact]
    public async Task SubmitRevision_MissingRelease_ShouldThrowValidationException()
    {
        using var context = CreateDbContext();
        var (swId, _, orgId, editorId, _) = await SeedBaseEntitiesAsync(context);
        var (service, _) = CreateServices(context);

        var createDto = new CreateDeploymentDto(
            swId,
            orgId,
            "Production",
            "default",
            null, // No release specified yet
            "NotInUse",
            null,
            null,
            editorId);

        var created = await service.CreateDeploymentAsync(createDto, editorId);
        var revId = created.ActiveRevision!.Id;

        // Submitting without release should throw CustomValidationException
        var act = async () => await service.SubmitRevisionAsync(revId, created.ActiveRevision.Version, editorId);
        var ex = await act.Should().ThrowAsync<CustomValidationException>();
        ex.Which.Errors.Should().ContainKey("ReleaseId");
    }

    [Fact]
    public async Task SubmitRevision_ActiveWithoutGoLiveDate_ShouldThrowValidationException()
    {
        using var context = CreateDbContext();
        var (swId, relId, orgId, editorId, _) = await SeedBaseEntitiesAsync(context);
        var (service, _) = CreateServices(context);

        var createDto = new CreateDeploymentDto(
            swId,
            orgId,
            "Production",
            "default",
            relId,
            "Active",
            new DateOnly(2025, 1, 1),
            null, // GoLiveDate is missing for Active status!
            editorId);

        var act = async () => await service.CreateDeploymentAsync(createDto, editorId);
        var ex = await act.Should().ThrowAsync<CustomValidationException>();
        ex.Which.Errors.Should().ContainKey("GoLiveDate");
    }

    [Fact]
    public async Task ApproveRevision_WhenSubmitterAttemptsSelfApproval_ShouldThrowForbiddenException()
    {
        using var context = CreateDbContext();
        var (swId, relId, orgId, editorId, _) = await SeedBaseEntitiesAsync(context);
        var (service, _) = CreateServices(context);

        var createDto = new CreateDeploymentDto(
            swId,
            orgId,
            "Production",
            "default",
            relId,
            "Active",
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 2, 1),
            editorId);

        var created = await service.CreateDeploymentAsync(createDto, editorId);
        var revId = created.ActiveRevision!.Id;

        var submitted = await service.SubmitRevisionAsync(revId, created.ActiveRevision.Version, editorId);
        submitted.WorkflowStatus.Should().Be("Submitted");

        // Anti-self-approval: Submitter tries to approve own submission
        var act = async () => await service.ApproveRevisionAsync(revId, submitted.Version, editorId);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*không được tự phê duyệt hồ sơ của chính mình*");
    }

    [Fact]
    public async Task FullWorkflow_Submit_Approve_CreateNextRevision_ShouldSucceed()
    {
        using var context = CreateDbContext();
        var (swId, relId, orgId, editorId, approverId) = await SeedBaseEntitiesAsync(context);
        var (service, _) = CreateServices(context);

        // 1. Create Deployment
        var createDto = new CreateDeploymentDto(
            swId,
            orgId,
            "Production",
            "default",
            relId,
            "Active",
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 2, 1),
            editorId);

        var created = await service.CreateDeploymentAsync(createDto, editorId);
        var rev1Id = created.ActiveRevision!.Id;

        // 2. Submit
        var submitted = await service.SubmitRevisionAsync(rev1Id, created.ActiveRevision.Version, editorId);
        submitted.WorkflowStatus.Should().Be("Submitted");

        // Check BackgroundJob was enqueued
        var jobCount = await context.BackgroundJobs.CountAsync();
        jobCount.Should().BeGreaterThanOrEqualTo(1);

        // 3. Approve by Approver
        var approved = await service.ApproveRevisionAsync(rev1Id, submitted.Version, approverId);
        approved.WorkflowStatus.Should().Be("Approved");
        approved.ApprovedAt.Should().NotBeNull();

        var deployment = await service.GetDeploymentByIdAsync(created.Id, approverId);
        deployment.CurrentApprovedRevisionId.Should().Be(rev1Id);
        deployment.CurrentApprovedRevision.Should().NotBeNull();

        // 4. Create Next Revision (revision_no = 2)
        var nextRev = await service.CreateNextRevisionAsync(deployment.Id, deployment.Version, editorId);
        nextRev.Should().NotBeNull();
        nextRev.RevisionNo.Should().Be(2);
        nextRev.WorkflowStatus.Should().Be("Draft");
        nextRev.OperationalStatus.Should().Be("Active");
    }

    [Fact]
    public async Task Reject_And_Reopen_ShouldTransitionCorrectly()
    {
        using var context = CreateDbContext();
        var (swId, relId, orgId, editorId, approverId) = await SeedBaseEntitiesAsync(context);
        var (service, _) = CreateServices(context);

        var createDto = new CreateDeploymentDto(
            swId,
            orgId,
            "Production",
            "default",
            relId,
            "Active",
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 2, 1),
            editorId);

        var created = await service.CreateDeploymentAsync(createDto, editorId);
        var revId = created.ActiveRevision!.Id;

        var submitted = await service.SubmitRevisionAsync(revId, created.ActiveRevision.Version, editorId);

        // Reject without reason fails
        var actEmptyReason = async () => await service.RejectRevisionAsync(revId, new RejectRevisionDto(""), submitted.Version, approverId);
        await actEmptyReason.Should().ThrowAsync<CustomValidationException>();

        // Reject with reason succeeds
        var rejected = await service.RejectRevisionAsync(revId, new RejectRevisionDto("Thông tin phân công cán bộ chưa đầy đủ"), submitted.Version, approverId);
        rejected.WorkflowStatus.Should().Be("Rejected");

        // Reopen by editor
        var reopened = await service.ReopenRejectedRevisionAsync(revId, rejected.Version, editorId);
        reopened.WorkflowStatus.Should().Be("Draft");
    }

    [Fact]
    public async Task UpdateDraftRevision_WithStaleVersion_ShouldThrowConcurrencyException()
    {
        using var context = CreateDbContext();
        var (swId, relId, orgId, editorId, _) = await SeedBaseEntitiesAsync(context);
        var (service, _) = CreateServices(context);

        var createDto = new CreateDeploymentDto(
            swId,
            orgId,
            "Production",
            "default",
            relId,
            "NotInUse",
            null,
            null,
            editorId);

        var created = await service.CreateDeploymentAsync(createDto, editorId);
        var revId = created.ActiveRevision!.Id;

        var staleVersion = 99999;
        var updateDto = new UpdateDraftRevisionDto(relId, "NotInUse", null, null, editorId);

        var act = async () => await service.UpdateDraftRevisionAsync(revId, updateDto, staleVersion, editorId);
        await act.Should().ThrowAsync<ConcurrencyException>();
    }
}
