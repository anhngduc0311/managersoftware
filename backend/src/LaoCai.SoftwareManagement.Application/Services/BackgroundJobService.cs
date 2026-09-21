using System.Text.Json;
using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Application.Common.Models;
using LaoCai.SoftwareManagement.Domain.Entities.Jobs;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IAppDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BackgroundJobService(IAppDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Guid> EnqueueAsync(string type, object payload, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var job = new BackgroundJob
        {
            Id = Guid.NewGuid(),
            Type = type,
            PayloadJson = JsonSerializer.Serialize(payload),
            Status = "Queued",
            Attempts = 0,
            MaxAttempts = 5,
            NextRunAt = nowUtc,
            CreatedAt = nowUtc
        };

        _context.BackgroundJobs.Add(job);
        // Note: SaveChangesAsync will be called either by the caller's transaction or here if needed
        return job.Id;
    }

    public async Task<PagedResult<BackgroundJobDto>> GetJobsAsync(
        int page,
        int pageSize,
        string? status = null,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.BackgroundJobs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(j => j.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(j => j.Type == type);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new BackgroundJobDto(
                j.Id,
                j.Type,
                j.PayloadJson,
                j.Status,
                j.Attempts,
                j.MaxAttempts,
                j.LeaseOwner,
                j.LeaseUntil,
                j.NextRunAt,
                j.CreatedAt,
                j.CompletedAt,
                j.LastError))
            .ToListAsync(cancellationToken);

        return PagedResult<BackgroundJobDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<BackgroundJobDto> GetJobByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var j = await _context.BackgroundJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (j == null)
            throw new NotFoundException("Tác vụ nền", id);

        return new BackgroundJobDto(
            j.Id,
            j.Type,
            j.PayloadJson,
            j.Status,
            j.Attempts,
            j.MaxAttempts,
            j.LeaseOwner,
            j.LeaseUntil,
            j.NextRunAt,
            j.CreatedAt,
            j.CompletedAt,
            j.LastError);
    }

    public async Task<bool> RetryJobAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _context.BackgroundJobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (job == null)
            throw new NotFoundException("Tác vụ nền", id);

        job.Status = "Queued";
        job.NextRunAt = _dateTimeProvider.UtcNow;
        job.Attempts = 0;
        job.LastError = null;
        job.LeaseOwner = null;
        job.LeaseToken = null;
        job.LeaseUntil = null;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
