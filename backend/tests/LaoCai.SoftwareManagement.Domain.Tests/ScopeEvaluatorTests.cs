using FluentAssertions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Application.Services;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
using LaoCai.SoftwareManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LaoCai.SoftwareManagement.Domain.Tests;

public class ScopeEvaluatorTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly MockCurrentUserService _currentUserService = new();

    public ScopeEvaluatorTests()
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
    public async Task HasPermissionAsync_WhenUserHasGlobalScope_ShouldAuthorizeAnyOrganization()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var orgService = new OrganizationService(context, dtProvider);
        var scopeService = new ScopeAuthorizationService(context, orgService, dtProvider);

        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permId = Guid.NewGuid();
        var targetOrgId = Guid.NewGuid();

        var user = new User { Id = userId, UserName = "admin", DisplayName = "Admin" };
        var perm = new Permission { Id = permId, Code = "catalog.manage", Name = "Manage Catalog", GroupName = "Catalog" };
        var role = new Role { Id = roleId, Code = "CatalogManager", Name = "Catalog Manager" };
        var rolePerm = new RolePermission { RoleId = roleId, PermissionId = permId, Role = role, Permission = perm };

        var grant = new UserRoleScope
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            Role = role,
            ScopeType = "Global",
            OrganizationId = null,
            IncludeDescendants = true,
            ValidFrom = dtProvider.UtcNow.AddDays(-10),
            ValidTo = null
        };

        context.Users.Add(user);
        context.Permissions.Add(perm);
        context.Roles.Add(role);
        context.RolePermissions.Add(rolePerm);
        context.UserRoleScopes.Add(grant);
        await context.SaveChangesAsync();

        var result = await scopeService.HasPermissionAsync(userId, "catalog.manage", targetOrgId);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserHasOrgScope_ShouldAuthorizeTargetOrg_AndRejectDifferentOrg()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var orgService = new OrganizationService(context, dtProvider);
        var scopeService = new ScopeAuthorizationService(context, orgService, dtProvider);

        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permId = Guid.NewGuid();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var user = new User { Id = userId, UserName = "editor", DisplayName = "Editor" };
        var perm = new Permission { Id = permId, Code = "deployments.write", Name = "Write Deployment", GroupName = "Deployments" };
        var role = new Role { Id = roleId, Code = "UnitEditor", Name = "Unit Editor" };
        var rolePerm = new RolePermission { RoleId = roleId, PermissionId = permId, Role = role, Permission = perm };

        var grant = new UserRoleScope
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            Role = role,
            ScopeType = "Organization",
            OrganizationId = orgA,
            IncludeDescendants = false,
            ValidFrom = dtProvider.UtcNow.AddDays(-10),
            ValidTo = null
        };

        context.Users.Add(user);
        context.Organizations.AddRange(
            new Organization { Id = orgA, Code = "ORG_A" },
            new Organization { Id = orgB, Code = "ORG_B" }
        );
        context.Permissions.Add(perm);
        context.Roles.Add(role);
        context.RolePermissions.Add(rolePerm);
        context.UserRoleScopes.Add(grant);
        await context.SaveChangesAsync();

        var canWriteOrgA = await scopeService.HasPermissionAsync(userId, "deployments.write", orgA);
        var canWriteOrgB = await scopeService.HasPermissionAsync(userId, "deployments.write", orgB);

        canWriteOrgA.Should().BeTrue();
        canWriteOrgB.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_WhenGrantIsExpired_ShouldRejectAccess()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var orgService = new OrganizationService(context, dtProvider);
        var scopeService = new ScopeAuthorizationService(context, orgService, dtProvider);

        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permId = Guid.NewGuid();
        var orgA = Guid.NewGuid();

        var user = new User { Id = userId, UserName = "editor", DisplayName = "Editor" };
        var perm = new Permission { Id = permId, Code = "deployments.write", Name = "Write Deployment", GroupName = "Deployments" };
        var role = new Role { Id = roleId, Code = "UnitEditor", Name = "Unit Editor" };
        var rolePerm = new RolePermission { RoleId = roleId, PermissionId = permId, Role = role, Permission = perm };

        var grant = new UserRoleScope
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            Role = role,
            ScopeType = "Organization",
            OrganizationId = orgA,
            ValidFrom = dtProvider.UtcNow.AddDays(-30),
            ValidTo = dtProvider.UtcNow.AddDays(-1) // Expired yesterday
        };

        context.Users.Add(user);
        context.Organizations.Add(new Organization { Id = orgA, Code = "ORG_A" });
        context.Permissions.Add(perm);
        context.Roles.Add(role);
        context.RolePermissions.Add(rolePerm);
        context.UserRoleScopes.Add(grant);
        await context.SaveChangesAsync();

        var result = await scopeService.HasPermissionAsync(userId, "deployments.write", orgA);
        result.Should().BeFalse();
    }
}
