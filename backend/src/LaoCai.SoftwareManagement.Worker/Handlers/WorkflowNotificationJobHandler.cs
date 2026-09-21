using System.Text.Json;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LaoCai.SoftwareManagement.Worker.Handlers;

public class WorkflowNotificationPayload
{
    public string Action { get; set; } = string.Empty;
    public Guid RevisionId { get; set; }
    public Guid DeploymentId { get; set; }
    public Guid OrganizationId { get; set; }
    public string SoftwareName { get; set; } = string.Empty;
    public Guid? SubmittedByUserId { get; set; }
    public Guid? ActorId { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
}

public class WorkflowNotificationJobHandler : IJobHandler
{
    private readonly IAppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<WorkflowNotificationJobHandler> _logger;

    public string JobType => "WorkflowNotification";

    public WorkflowNotificationJobHandler(
        IAppDbContext context,
        INotificationService notificationService,
        ILogger<WorkflowNotificationJobHandler> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<WorkflowNotificationPayload>(payloadJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (payload == null)
        {
            _logger.LogWarning("Payload của WorkflowNotificationJob rỗng hoặc không hợp lệ: {PayloadJson}", payloadJson);
            return;
        }

        _logger.LogInformation("Xử lý thông báo luồng duyệt: {Action} cho phần mềm {SoftwareName}", payload.Action, payload.SoftwareName);

        switch (payload.Action)
        {
            case "Submitted":
                await HandleSubmittedAsync(payload, cancellationToken);
                break;

            case "Approved":
                await HandleApprovedAsync(payload, cancellationToken);
                break;

            case "Rejected":
                await HandleRejectedAsync(payload, cancellationToken);
                break;

            default:
                _logger.LogWarning("Không hỗ trợ action luồng duyệt: {Action}", payload.Action);
                break;
        }
    }

    private async Task HandleSubmittedAsync(WorkflowNotificationPayload payload, CancellationToken cancellationToken)
    {
        // Find users with deployments.approve in this org or Global
        var approverUsers = await _context.UserRoleScopes
            .AsNoTracking()
            .Where(urs =>
                (urs.ScopeType == "Global" || (urs.OrganizationId == payload.OrganizationId)) &&
                urs.Role.RolePermissions.Any(rp => rp.Permission.Code == "deployments.approve"))
            .Select(urs => urs.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var targetRoute = $"/deployments/{payload.DeploymentId}";

        foreach (var userId in approverUsers)
        {
            // Do not notify the submitter to approve their own
            if (payload.SubmittedByUserId.HasValue && userId == payload.SubmittedByUserId.Value)
                continue;

            var dedupeKey = $"wf:submitted:{payload.RevisionId}:{userId}";
            await _notificationService.CreateNotificationAsync(
                recipientUserId: userId,
                type: "WorkflowSubmitted",
                title: $"Yêu cầu phê duyệt: {payload.SoftwareName}",
                message: $"Hồ sơ triển khai phần mềm '{payload.SoftwareName}' đã được gửi duyệt. Vui lòng kiểm tra và phê duyệt.",
                targetRoute: targetRoute,
                deduplicationKey: dedupeKey,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleApprovedAsync(WorkflowNotificationPayload payload, CancellationToken cancellationToken)
    {
        if (!payload.SubmittedByUserId.HasValue)
            return;

        var targetRoute = $"/deployments/{payload.DeploymentId}";
        var dedupeKey = $"wf:approved:{payload.RevisionId}:{payload.SubmittedByUserId.Value}";

        await _notificationService.CreateNotificationAsync(
            recipientUserId: payload.SubmittedByUserId.Value,
            type: "WorkflowApproved",
            title: $"Hồ sơ đã được phê duyệt: {payload.SoftwareName}",
            message: $"Hồ sơ triển khai phần mềm '{payload.SoftwareName}' đã được lãnh đạo phê duyệt chính thức.",
            targetRoute: targetRoute,
            deduplicationKey: dedupeKey,
            cancellationToken: cancellationToken);
    }

    private async Task HandleRejectedAsync(WorkflowNotificationPayload payload, CancellationToken cancellationToken)
    {
        if (!payload.SubmittedByUserId.HasValue)
            return;

        var targetRoute = $"/deployments/{payload.DeploymentId}";
        var dedupeKey = $"wf:rejected:{payload.RevisionId}:{payload.SubmittedByUserId.Value}";

        var reasonText = string.IsNullOrWhiteSpace(payload.Reason) ? "Không có lý do cụ thể" : payload.Reason;

        await _notificationService.CreateNotificationAsync(
            recipientUserId: payload.SubmittedByUserId.Value,
            type: "WorkflowRejected",
            title: $"Hồ sơ bị trả lại: {payload.SoftwareName}",
            message: $"Hồ sơ triển khai phần mềm '{payload.SoftwareName}' bị từ chối phê duyệt. Lý do: {reasonText}",
            targetRoute: targetRoute,
            deduplicationKey: dedupeKey,
            cancellationToken: cancellationToken);
    }
}
