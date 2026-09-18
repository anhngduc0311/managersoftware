using LaoCai.SoftwareManagement.Domain.Entities.Audit;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserRoleScope> UserRoleScopes { get; }
    DbSet<AuditLog> AuditLogs { get; }

    // Organizations Schema
    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationVersion> OrganizationVersions { get; }
    DbSet<OrganizationSuccession> OrganizationSuccessions { get; }

    // Catalog Schema
    DbSet<SoftwareCategory> SoftwareCategories { get; }
    DbSet<Vendor> Vendors { get; }
    DbSet<Software> Software { get; }
    DbSet<SoftwareRelease> SoftwareReleases { get; }
    DbSet<CatalogProposal> CatalogProposals { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? CorrelationId { get; }
    bool IsAuthenticated { get; }
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
