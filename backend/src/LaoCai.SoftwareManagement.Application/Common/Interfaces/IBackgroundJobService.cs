using LaoCai.SoftwareManagement.Application.Common.Models;

namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public record BackgroundJobDto(
    Guid Id,
    string Type,
    string PayloadJson,
    string Status,
    int Attempts,
    int MaxAttempts,
    string? LeaseOwner,
    DateTime? LeaseUntil,
    DateTime NextRunAt,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? LastError);

public interface IBackgroundJobService
{
    Task<Guid> EnqueueAsync(string type, object payload, CancellationToken cancellationToken = default);
    Task<PagedResult<BackgroundJobDto>> GetJobsAsync(int page, int pageSize, string? status = null, string? type = null, CancellationToken cancellationToken = default);
    Task<BackgroundJobDto> GetJobByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RetryJobAsync(Guid id, CancellationToken cancellationToken = default);
}
