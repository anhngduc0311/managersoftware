namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public record ReminderExecutionResult(
    int TotalScanned,
    int RemindersSent,
    List<string> Details);

public interface IExpirationReminderService
{
    Task<ReminderExecutionResult> ProcessRemindersAsync(CancellationToken cancellationToken = default);
}
