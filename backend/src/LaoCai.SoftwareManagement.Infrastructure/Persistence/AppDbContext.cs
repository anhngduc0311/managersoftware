using System.Text.Json;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Common;
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
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LaoCai.SoftwareManagement.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRoleScope> UserRoleScopes => Set<UserRoleScope>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Organizations Schema
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationVersion> OrganizationVersions => Set<OrganizationVersion>();
    public DbSet<OrganizationSuccession> OrganizationSuccessions => Set<OrganizationSuccession>();

    // Catalog Schema
    public DbSet<SoftwareCategory> SoftwareCategories => Set<SoftwareCategory>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Software> Software => Set<Software>();
    public DbSet<SoftwareRelease> SoftwareReleases => Set<SoftwareRelease>();
    public DbSet<CatalogProposal> CatalogProposals => Set<CatalogProposal>();

    // Deployments Schema
    public DbSet<Deployment> Deployments => Set<Deployment>();
    public DbSet<DeploymentRevision> DeploymentRevisions => Set<DeploymentRevision>();
    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();

    // Jobs & Idempotency Schema
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    // Notifications Schema
    public DbSet<Notification> Notifications => Set<Notification>();

    // Contracts Schema
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractItem> ContractItems => Set<ContractItem>();
    public DbSet<LicenseEntitlement> LicenseEntitlements => Set<LicenseEntitlement>();
    public DbSet<LicenseAllocation> LicenseAllocations => Set<LicenseAllocation>();

    // Documents Schema
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentAttachment> DocumentAttachments => Set<DocumentAttachment>();

    // Reports Schema (Phase 5)
    public DbSet<CoverageEligibility> CoverageEligibilities => Set<CoverageEligibility>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ImportRowError> ImportRowErrors => Set<ImportRowError>();
    public DbSet<ExportRequest> ExportRequests => Set<ExportRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // IAM Schema Configurations
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users", "iam");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.UserName).HasMaxLength(100).IsRequired();
            builder.HasIndex(u => u.NormalizedUserName).IsUnique();
            builder.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(u => u.Email).HasMaxLength(256);
            builder.Property(u => u.SecurityStamp).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<Role>(builder =>
        {
            builder.ToTable("roles", "iam");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Code).HasMaxLength(50).IsRequired();
            builder.HasIndex(r => r.Code).IsUnique();
            builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Permission>(builder =>
        {
            builder.ToTable("permissions", "iam");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Code).HasMaxLength(100).IsRequired();
            builder.HasIndex(p => p.Code).IsUnique();
            builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
            builder.Property(p => p.GroupName).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<RolePermission>(builder =>
        {
            builder.ToTable("role_permissions", "iam");
            builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            builder.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            builder.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);
        });

        modelBuilder.Entity<UserRoleScope>(builder =>
        {
            builder.ToTable("user_role_scopes", "iam");
            builder.HasKey(urs => urs.Id);
            builder.Property(urs => urs.ScopeType).HasMaxLength(20).IsRequired();

            builder.HasOne(urs => urs.User)
                .WithMany(u => u.RoleScopes)
                .HasForeignKey(urs => urs.UserId);

            builder.HasOne(urs => urs.Role)
                .WithMany()
                .HasForeignKey(urs => urs.RoleId);
        });

        // Audit Schema Configuration
        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("audit_logs", "audit");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
            builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            builder.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
            builder.Property(a => a.CorrelationId).HasMaxLength(100);
            builder.HasIndex(a => new { a.OrganizationId, a.OccurredAt });
            builder.HasIndex(a => new { a.EntityType, a.EntityId, a.OccurredAt });
        });

        // Organizations Schema Configurations
        modelBuilder.Entity<Organization>(builder =>
        {
            builder.ToTable("organizations", "organizations");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Code).HasMaxLength(50).IsRequired();
            builder.HasIndex(o => o.Code).IsUnique();

            builder.HasMany(o => o.Versions)
                .WithOne(v => v.Organization)
                .HasForeignKey(v => v.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationVersion>(builder =>
        {
            builder.ToTable("organization_versions", "organizations");
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Name).HasMaxLength(255).IsRequired();

            builder.HasOne(v => v.Parent)
                .WithMany()
                .HasForeignKey(v => v.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(v => new { v.OrganizationId, v.ValidFrom });
        });

        modelBuilder.Entity<OrganizationSuccession>(builder =>
        {
            builder.ToTable("organization_successions", "organizations");
            builder.HasKey(s => s.Id);

            builder.HasOne(s => s.Predecessor)
                .WithMany()
                .HasForeignKey(s => s.PredecessorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Successor)
                .WithMany()
                .HasForeignKey(s => s.SuccessorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Catalog Schema Configurations
        modelBuilder.Entity<SoftwareCategory>(builder =>
        {
            builder.ToTable("software_categories", "catalog");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
            builder.HasIndex(c => c.Code).IsUnique();
            builder.Property(c => c.Name).HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<Vendor>(builder =>
        {
            builder.ToTable("vendors", "catalog");
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Code).HasMaxLength(50).IsRequired();
            builder.HasIndex(v => v.Code).IsUnique();
            builder.Property(v => v.Name).HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<Software>(builder =>
        {
            builder.ToTable("software", "catalog");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Code).HasMaxLength(50).IsRequired();
            builder.HasIndex(s => s.Code).IsUnique();
            builder.Property(s => s.Name).HasMaxLength(255).IsRequired();
            builder.Property(s => s.LifecycleStatus).HasMaxLength(50).IsRequired();

            builder.HasOne(s => s.Category)
                .WithMany(c => c.SoftwareList)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Vendor)
                .WithMany(v => v.SoftwareList)
                .HasForeignKey(s => s.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Releases)
                .WithOne(r => r.Software)
                .HasForeignKey(r => r.SoftwareId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SoftwareRelease>(builder =>
        {
            builder.ToTable("software_releases", "catalog");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.VersionName).HasMaxLength(50).IsRequired();
            builder.HasIndex(r => new { r.SoftwareId, r.VersionName }).IsUnique();
        });

        modelBuilder.Entity<CatalogProposal>(builder =>
        {
            builder.ToTable("catalog_proposals", "catalog");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.SoftwareName).HasMaxLength(255).IsRequired();
            builder.Property(p => p.Status).HasMaxLength(30).IsRequired();

            builder.HasOne(p => p.Organization)
                .WithMany()
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ProposedByUser)
                .WithMany()
                .HasForeignKey(p => p.ProposedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ReviewedByUser)
                .WithMany()
                .HasForeignKey(p => p.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.CreatedSoftware)
                .WithMany()
                .HasForeignKey(p => p.CreatedSoftwareId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Deployments Schema Configurations
        modelBuilder.Entity<Deployment>(builder =>
        {
            builder.ToTable("deployments", "deployments");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Environment).HasMaxLength(50).IsRequired();
            builder.Property(d => d.InstanceKey).HasMaxLength(50).HasDefaultValue("default").IsRequired();
            builder.HasIndex(d => new { d.SoftwareId, d.OrganizationId, d.Environment, d.InstanceKey }).IsUnique();
            builder.HasIndex(d => new { d.OrganizationId, d.SoftwareId });

            builder.HasOne(d => d.Software)
                .WithMany()
                .HasForeignKey(d => d.SoftwareId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.Organization)
                .WithMany()
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.CurrentApprovedRevision)
                .WithMany()
                .HasForeignKey(d => d.CurrentApprovedRevisionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(d => d.Revisions)
                .WithOne(r => r.Deployment)
                .HasForeignKey(r => r.DeploymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeploymentRevision>(builder =>
        {
            builder.ToTable("deployment_revisions", "deployments");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.OperationalStatus).HasMaxLength(50).IsRequired();
            builder.Property(r => r.WorkflowStatus).HasMaxLength(30).HasDefaultValue("Draft").IsRequired();

            builder.HasIndex(r => new { r.DeploymentId, r.RevisionNo }).IsUnique();
            builder.HasIndex(r => new { r.WorkflowStatus, r.DeploymentId });
            builder.HasIndex(r => new { r.DeploymentId, r.ApprovedAt });
            builder.HasIndex(r => new { r.OperationalStatus, r.ApprovedAt });

            // Partial unique index: at most one active revision (Draft or Submitted) per deployment
            builder.HasIndex(r => r.DeploymentId)
                .HasFilter("\"workflow_status\" IN ('Draft', 'Submitted')")
                .IsUnique()
                .HasDatabaseName("ix_deployment_revisions_active");

            builder.HasOne(r => r.Release)
                .WithMany()
                .HasForeignKey(r => r.ReleaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.ResponsibleUser)
                .WithMany()
                .HasForeignKey(r => r.ResponsibleUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(r => r.SubmittedByUser)
                .WithMany()
                .HasForeignKey(r => r.SubmittedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(r => r.Decisions)
                .WithOne(d => d.DeploymentRevision)
                .HasForeignKey(d => d.DeploymentRevisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApprovalDecision>(builder =>
        {
            builder.ToTable("approval_decisions", "deployments");
            builder.HasKey(ad => ad.Id);
            builder.Property(ad => ad.Decision).HasMaxLength(30).IsRequired();
            builder.Property(ad => ad.Reason).HasMaxLength(2000);

            builder.HasOne(ad => ad.Actor)
                .WithMany()
                .HasForeignKey(ad => ad.ActorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Jobs & Idempotency Schema Configurations
        modelBuilder.Entity<BackgroundJob>(builder =>
        {
            builder.ToTable("background_jobs", "jobs");
            builder.HasKey(j => j.Id);
            builder.Property(j => j.Type).HasMaxLength(100).IsRequired();
            builder.Property(j => j.Status).HasMaxLength(30).HasDefaultValue("Queued").IsRequired();
            builder.Property(j => j.LeaseOwner).HasMaxLength(100);
            builder.HasIndex(j => new { j.Status, j.NextRunAt, j.LeaseUntil });
        });

        modelBuilder.Entity<IdempotencyRecord>(builder =>
        {
            builder.ToTable("idempotency_records", "jobs");
            builder.HasKey(r => r.Key);
            builder.Property(r => r.Key).HasMaxLength(100);
            builder.Property(r => r.RequestPath).HasMaxLength(200).IsRequired();
            builder.Property(r => r.RequestHash).HasMaxLength(64).IsRequired();
        });

        // Notifications Schema Configurations
        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("notifications", "notifications");
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
            builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
            builder.Property(n => n.TargetRoute).HasMaxLength(500);
            builder.Property(n => n.DeduplicationKey).HasMaxLength(200);

            builder.HasIndex(n => n.DeduplicationKey)
                .IsUnique()
                .HasFilter("\"deduplication_key\" IS NOT NULL");

            builder.HasIndex(n => new { n.RecipientUserId, n.ReadAt, n.CreatedAt });

            builder.HasOne(n => n.RecipientUser)
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Contracts Schema Configurations
        modelBuilder.Entity<Contract>(builder =>
        {
            builder.ToTable("contracts", "contracts");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.ContractNo).HasMaxLength(100).IsRequired();
            builder.Property(c => c.Status).HasMaxLength(30).HasDefaultValue("Active").IsRequired();
            builder.Property(c => c.TotalAmount).HasPrecision(18, 2);
            builder.Property(c => c.CurrencyCode).HasMaxLength(3).HasDefaultValue("VND").IsRequired();
            builder.Property(c => c.Version).IsConcurrencyToken();

            builder.HasIndex(c => c.ContractNo);
            builder.HasIndex(c => new { c.OwningOrganizationId, c.Status });
            builder.HasIndex(c => new { c.VendorId, c.Status });
            builder.HasIndex(c => new { c.Status, c.EndDate });

            builder.HasOne(c => c.OwningOrganization)
                .WithMany()
                .HasForeignKey(c => c.OwningOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Vendor)
                .WithMany()
                .HasForeignKey(c => c.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Items)
                .WithOne(i => i.Contract)
                .HasForeignKey(i => i.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContractItem>(builder =>
        {
            builder.ToTable("contract_items", "contracts");
            builder.HasKey(ci => ci.Id);
            builder.Property(ci => ci.Description).HasMaxLength(500);
            builder.Property(ci => ci.Amount).HasPrecision(18, 2);

            builder.HasOne(ci => ci.Software)
                .WithMany()
                .HasForeignKey(ci => ci.SoftwareId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(ci => ci.Entitlements)
                .WithOne(e => e.ContractItem)
                .HasForeignKey(e => e.ContractItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LicenseEntitlement>(builder =>
        {
            builder.ToTable("license_entitlements", "contracts");
            builder.HasKey(le => le.Id);
            builder.Property(le => le.LicenseType).HasMaxLength(50).IsRequired();
            builder.HasIndex(le => new { le.ValidTo, le.LicenseType });

            builder.HasMany(le => le.Allocations)
                .WithOne(a => a.Entitlement)
                .HasForeignKey(a => a.EntitlementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LicenseAllocation>(builder =>
        {
            builder.ToTable("license_allocations", "contracts");
            builder.HasKey(la => la.Id);

            builder.HasIndex(la => new { la.EntitlementId, la.DeploymentId }).IsUnique();

            builder.HasOne(la => la.Deployment)
                .WithMany()
                .HasForeignKey(la => la.DeploymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Documents Schema Configurations
        modelBuilder.Entity<Document>(builder =>
        {
            builder.ToTable("documents", "documents");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.StorageKey).HasMaxLength(255).IsRequired();
            builder.HasIndex(d => d.StorageKey).IsUnique();

            builder.Property(d => d.OriginalName).HasMaxLength(255).IsRequired();
            builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
            builder.Property(d => d.ChecksumSha256).HasMaxLength(64).IsRequired();
            builder.Property(d => d.ScanStatus).HasMaxLength(30).HasDefaultValue("Pending").IsRequired();

            builder.HasIndex(d => d.ScanStatus);
            builder.HasIndex(d => d.CreatedAt);

            builder.HasOne(d => d.UploadedByUser)
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(d => d.Attachments)
                .WithOne(a => a.Document)
                .HasForeignKey(a => a.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentAttachment>(builder =>
        {
            builder.ToTable("document_attachments", "documents");
            builder.HasKey(da => da.Id);
            builder.Property(da => da.EntityType).HasMaxLength(50).IsRequired();

            builder.HasIndex(da => new { da.DocumentId, da.EntityType, da.EntityId }).IsUnique();
            builder.HasIndex(da => new { da.EntityType, da.EntityId });

            builder.HasOne(da => da.AttachedByUser)
                .WithMany()
                .HasForeignKey(da => da.AttachedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Reports Schema Configurations (Phase 5)
        modelBuilder.Entity<CoverageEligibility>(builder =>
        {
            builder.ToTable("coverage_eligibility", "reports");
            builder.HasKey(ce => ce.Id);

            builder.HasIndex(ce => new { ce.OrganizationId, ce.SoftwareId, ce.ValidFrom });

            builder.HasOne(ce => ce.Organization)
                .WithMany()
                .HasForeignKey(ce => ce.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ce => ce.Software)
                .WithMany()
                .HasForeignKey(ce => ce.SoftwareId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ImportBatch>(builder =>
        {
            builder.ToTable("import_batches", "reports");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Status).HasMaxLength(30).HasDefaultValue("Pending").IsRequired();

            builder.HasIndex(b => b.Status);
            builder.HasIndex(b => b.RequestedByUserId);

            builder.HasMany(b => b.RowErrors)
                .WithOne(re => re.Batch)
                .HasForeignKey(re => re.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportRowError>(builder =>
        {
            builder.ToTable("import_row_errors", "reports");
            builder.HasKey(re => re.Id);
            builder.Property(re => re.ColumnName).HasMaxLength(100).IsRequired();
            builder.Property(re => re.ErrorCode).HasMaxLength(100).IsRequired();
            builder.Property(re => re.ErrorMessage).HasMaxLength(500).IsRequired();

            builder.HasIndex(re => new { re.BatchId, re.RowIndex });
        });

        modelBuilder.Entity<ExportRequest>(builder =>
        {
            builder.ToTable("export_requests", "reports");
            builder.HasKey(er => er.Id);
            builder.Property(er => er.ExportType).HasMaxLength(50).IsRequired();
            builder.Property(er => er.Status).HasMaxLength(30).HasDefaultValue("Queued").IsRequired();

            builder.HasIndex(er => er.Status);
            builder.HasIndex(er => er.RequestedByUserId);
            builder.HasIndex(er => er.ExpiresAt);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var currentUserId = _currentUserService.UserId;
        var correlationId = _currentUserService.CorrelationId;

        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = nowUtc;
                    auditable.CreatedBy = currentUserId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = nowUtc;
                    auditable.UpdatedBy = currentUserId;
                }
            }

            if (entry.Entity is IVersionedEntity versioned && entry.State == EntityState.Modified)
            {
                versioned.Version += 1;
            }

            // Enforce append-only for AuditLog
            if (entry.Entity is AuditLog && (entry.State == EntityState.Modified || entry.State == EntityState.Deleted))
            {
                throw new InvalidOperationException("Nhật ký kiểm toán (AuditLog) là dữ liệu chỉ ghi (Append-Only), không cho phép sửa hoặc xóa.");
            }

            // Create AuditLog entry for changes (except AuditLog itself)
            if (entry.Entity is not AuditLog &&
                (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted))
            {
                var auditLog = CreateAuditLogEntry(entry, currentUserId, correlationId, nowUtc);
                if (auditLog != null)
                {
                    auditEntries.Add(auditLog);
                }
            }
        }

        if (auditEntries.Count > 0)
        {
            AuditLogs.AddRange(auditEntries);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static AuditLog? CreateAuditLogEntry(
        EntityEntry entry,
        Guid? currentUserId,
        string? correlationId,
        DateTime nowUtc)
    {
        var entityType = entry.Entity.GetType().Name;
        var entityIdProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        var entityId = entityIdProperty?.CurrentValue?.ToString() ?? Guid.NewGuid().ToString();

        var action = entry.State switch
        {
            EntityState.Added => "Create",
            EntityState.Modified => "Update",
            EntityState.Deleted => "Delete",
            _ => "Unknown"
        };

        var beforeValues = new Dictionary<string, object?>();
        var afterValues = new Dictionary<string, object?>();

        var sensitiveFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash", "SecurityStamp", "Token", "Secret", "PrivateKey"
        };

        foreach (var prop in entry.Properties)
        {
            var propName = prop.Metadata.Name;
            if (sensitiveFields.Contains(propName))
                continue;

            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                beforeValues[propName] = prop.OriginalValue;
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                afterValues[propName] = prop.CurrentValue;
            }
        }

        Guid? organizationId = null;
        var orgProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "OrganizationId");
        if (orgProp?.CurrentValue is Guid orgId)
        {
            organizationId = orgId;
        }

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = currentUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OrganizationId = organizationId,
            BeforeJson = beforeValues.Count > 0 ? JsonSerializer.Serialize(beforeValues) : null,
            AfterJson = afterValues.Count > 0 ? JsonSerializer.Serialize(afterValues) : null,
            OccurredAt = nowUtc,
            CorrelationId = correlationId
        };
    }
}
