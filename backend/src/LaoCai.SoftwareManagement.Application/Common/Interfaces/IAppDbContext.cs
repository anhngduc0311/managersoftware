using LaoCai.SoftwareManagement.Domain.Entities.Audit;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
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
