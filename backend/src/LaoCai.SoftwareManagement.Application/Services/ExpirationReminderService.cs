using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LaoCai.SoftwareManagement.Application.Services;

public class ExpirationReminderService : IExpirationReminderService
{
    private readonly IAppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IScopeAuthorizationService _scopeAuth;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ExpirationReminderService> _logger;

    private static readonly int[] ReminderThresholds = { 30, 15, 7 };

    public ExpirationReminderService(
        IAppDbContext context,
        INotificationService notificationService,
        IScopeAuthorizationService scopeAuth,
        IDateTimeProvider dateTimeProvider,
        ILogger<ExpirationReminderService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _scopeAuth = scopeAuth;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<ReminderExecutionResult> ProcessRemindersAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var details = new List<string>();
        int totalScanned = 0;
        int remindersSent = 0;

        // 1. Get eligible recipient users (SystemAdmin, Coordinator, or users with contracts.read / reports.read)
        var activeUsers = await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .Include(u => u.RoleScopes)
                .ThenInclude(rs => rs.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        // 2. Scan Contracts for EndDate expiration
        var activeContracts = await _context.Contracts
            .AsNoTracking()
            .Include(c => c.OwningOrganization)
                .ThenInclude(o => o.Versions)
            .Where(c => c.Status == "Active")
            .ToListAsync(cancellationToken);

        totalScanned += activeContracts.Count;

        foreach (var contract in activeContracts)
        {
            var orgName = contract.OwningOrganization.Versions.OrderByDescending(v => v.ValidFrom).Select(v => v.Name).FirstOrDefault() ?? contract.OwningOrganization.Code;
            var daysRemaining = contract.EndDate.DayNumber - today.DayNumber;
            var bucket = DetermineBucket(daysRemaining);

            if (bucket.HasValue)
            {
                var recipients = GetEligibleRecipients(activeUsers, contract.OwningOrganizationId, "contracts.read");

                foreach (var user in recipients)
                {
                    var dedupeKey = $"reminder:contract:{contract.Id}:{bucket.Value}:{user.Id}";
                    var title = $"[Nhắc hạn] Hợp đồng số {contract.ContractNo} sắp hết hạn ({bucket.Value} ngày)";
                    var message = $"Hợp đồng số '{contract.ContractNo}' của đơn vị '{orgName}' sẽ hết hiệu lực vào ngày {contract.EndDate:dd/MM/yyyy} (còn {daysRemaining} ngày).";

                    var notifId = await _notificationService.CreateNotificationAsync(
                        user.Id,
                        "ContractExpiration",
                        title,
                        message,
                        $"/contracts/{contract.Id}",
                        dedupeKey,
                        cancellationToken);

                    remindersSent++;
                    details.Add($"Contract {contract.ContractNo} -> User {user.UserName} (Threshold {bucket.Value}d)");
                }
            }

            // 3. Scan Maintenance EndDate
            if (contract.MaintenanceEndDate.HasValue)
            {
                var mDaysRemaining = contract.MaintenanceEndDate.Value.DayNumber - today.DayNumber;
                var mBucket = DetermineBucket(mDaysRemaining);

                if (mBucket.HasValue)
                {
                    var recipients = GetEligibleRecipients(activeUsers, contract.OwningOrganizationId, "contracts.read");

                    foreach (var user in recipients)
                    {
                        var dedupeKey = $"reminder:maintenance:{contract.Id}:{mBucket.Value}:{user.Id}";
                        var title = $"[Nhắc hạn] Hạn bảo trì hợp đồng {contract.ContractNo} sắp hết ({mBucket.Value} ngày)";
                        var message = $"Hạn bảo trì cho hợp đồng '{contract.ContractNo}' ({orgName}) sẽ kết thúc vào ngày {contract.MaintenanceEndDate.Value:dd/MM/yyyy} (còn {mDaysRemaining} ngày).";

                        await _notificationService.CreateNotificationAsync(
                            user.Id,
                            "MaintenanceExpiration",
                            title,
                            message,
                            $"/contracts/{contract.Id}",
                            dedupeKey,
                            cancellationToken);

                        remindersSent++;
                        details.Add($"Maintenance {contract.ContractNo} -> User {user.UserName} (Threshold {mBucket.Value}d)");
                    }
                }
            }
        }

        // 4. Scan License Entitlements
        var activeEntitlements = await _context.LicenseEntitlements
            .AsNoTracking()
            .Include(e => e.ContractItem)
                .ThenInclude(ci => ci.Contract)
                    .ThenInclude(c => c.OwningOrganization)
                        .ThenInclude(o => o.Versions)
            .Include(e => e.ContractItem)
                .ThenInclude(ci => ci.Software)
            .Where(e => e.ContractItem.Contract.Status == "Active")
            .ToListAsync(cancellationToken);

        totalScanned += activeEntitlements.Count;

        foreach (var entitlement in activeEntitlements)
        {
            var daysRemaining = entitlement.ValidTo.DayNumber - today.DayNumber;
            var bucket = DetermineBucket(daysRemaining);

            if (bucket.HasValue)
            {
                var org = entitlement.ContractItem.Contract.OwningOrganization;
                var entOrgName = org.Versions.OrderByDescending(v => v.ValidFrom).Select(v => v.Name).FirstOrDefault() ?? org.Code;
                var sw = entitlement.ContractItem.Software;
                var recipients = GetEligibleRecipients(activeUsers, org.Id, "licenses.allocate");

                foreach (var user in recipients)
                {
                    var dedupeKey = $"reminder:license:{entitlement.Id}:{bucket.Value}:{user.Id}";
                    var title = $"[Nhắc hạn] Giấy phép phần mềm {sw.Name} sắp hết hạn ({bucket.Value} ngày)";
                    var message = $"Giấy phép phần mềm '{sw.Name}' thuộc đơn vị '{entOrgName}' sẽ hết hạn vào ngày {entitlement.ValidTo:dd/MM/yyyy} (còn {daysRemaining} ngày).";

                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        "LicenseExpiration",
                        title,
                        message,
                        $"/contracts/{entitlement.ContractItem.ContractId}",
                        dedupeKey,
                        cancellationToken);

                    remindersSent++;
                    details.Add($"License {sw.Name} -> User {user.UserName} (Threshold {bucket.Value}d)");
                }
            }
        }

        _logger.LogInformation("Hoàn tất quét nhắc hạn: {Scanned} thực thể, {Sent} thông báo đã xử lý.", totalScanned, remindersSent);

        return new ReminderExecutionResult(totalScanned, remindersSent, details);
    }

    private static int? DetermineBucket(int daysRemaining)
    {
        if (daysRemaining <= 0)
            return null; // Overdue or expired handled separately

        if (daysRemaining <= 7)
            return 7;
        if (daysRemaining <= 15)
            return 15;
        if (daysRemaining <= 30)
            return 30;

        return null;
    }

    private static List<Domain.Entities.Iam.User> GetEligibleRecipients(
        List<Domain.Entities.Iam.User> users,
        Guid targetOrgId,
        string requiredPermission)
    {
        return users.Where(u =>
        {
            // Global roles (SysAdmin or Coordinator) or role with required permission
            var hasGlobalOrOrgPermission = u.RoleScopes.Any(rs =>
                (rs.Role.Code == "SystemAdmin" || rs.Role.Code == "Coordinator" ||
                 rs.Role.RolePermissions.Any(rp => rp.Permission.Code == requiredPermission || rp.Permission.Code == "reports.read")) &&
                (rs.ScopeType == "Global" || (rs.ScopeType == "Organization" && rs.OrganizationId == targetOrgId))
            );

            return hasGlobalOrOrgPermission;
        }).ToList();
    }
}
