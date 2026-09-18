using System.Text.Json;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Common;
using LaoCai.SoftwareManagement.Domain.Entities.Audit;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
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
