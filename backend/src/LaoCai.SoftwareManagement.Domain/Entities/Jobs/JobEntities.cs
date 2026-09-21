using LaoCai.SoftwareManagement.Domain.Common;

namespace LaoCai.SoftwareManagement.Domain.Entities.Jobs;

public class BackgroundJob : Entity<Guid>
{
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = "Queued"; // Queued | Running | Succeeded | Failed
    public int Attempts { get; set; } = 0;
    public int MaxAttempts { get; set; } = 5;

    public string? LeaseOwner { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTime? LeaseUntil { get; set; }

    public DateTime NextRunAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}

public class IdempotencyRecord
{
    public string Key { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string RequestPath { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
