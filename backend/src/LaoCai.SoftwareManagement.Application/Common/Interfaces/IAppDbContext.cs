using LaoCai.SoftwareManagement.Domain.Entities.Audit;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using LaoCai.SoftwareManagement.Domain.Entities.Contracts;
using LaoCai.SoftwareManagement.Domain.Entities.Deployments;
using LaoCai.SoftwareManagement.Domain.Entities.Documents;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using LaoCai.SoftwareManagement.Domain.Entities.Jobs;
using LaoCai.SoftwareManagement.Domain.Entities.Notifications;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
using LaoCai.SoftwareManagement.Domain.Entities.Reports;
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

    // Deployments Schema
    DbSet<Deployment> Deployments { get; }
    DbSet<DeploymentRevision> DeploymentRevisions { get; }
    DbSet<ApprovalDecision> ApprovalDecisions { get; }

    // Jobs & Idempotency Schema
    DbSet<BackgroundJob> BackgroundJobs { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    // Notifications Schema
    DbSet<Notification> Notifications { get; }

    // Contracts Schema
    DbSet<Contract> Contracts { get; }
    DbSet<ContractItem> ContractItems { get; }
    DbSet<LicenseEntitlement> LicenseEntitlements { get; }
    DbSet<LicenseAllocation> LicenseAllocations { get; }

    // Documents Schema
    DbSet<Document> Documents { get; }
    DbSet<DocumentAttachment> DocumentAttachments { get; }

    // Reports Schema (Phase 5)
    DbSet<CoverageEligibility> CoverageEligibilities { get; }
    DbSet<ImportBatch> ImportBatches { get; }
    DbSet<ImportRowError> ImportRowErrors { get; }
    DbSet<ExportRequest> ExportRequests { get; }

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
