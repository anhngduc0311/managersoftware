using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LaoCai.SoftwareManagement.Worker.Handlers;

public class ExpirationReminderJobHandler : IJobHandler
{
    private readonly IExpirationReminderService _reminderService;
    private readonly ILogger<ExpirationReminderJobHandler> _logger;

    public string JobType => "reminder.expiration";

    public ExpirationReminderJobHandler(
        IExpirationReminderService reminderService,
        ILogger<ExpirationReminderJobHandler> logger)
    {
        _reminderService = reminderService;
        _logger = logger;
    }

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bắt đầu tiến trình quét và gửi nhắc hạn hợp đồng / bản quyền định kỳ...");
        var result = await _reminderService.ProcessRemindersAsync(cancellationToken);
        _logger.LogInformation("Hoàn tất quét nhắc hạn: {Total} thực thể quét, {Sent} thông báo tạo.", result.TotalScanned, result.RemindersSent);
    }
}
