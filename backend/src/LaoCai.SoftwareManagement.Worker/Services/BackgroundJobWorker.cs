using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Jobs;
using LaoCai.SoftwareManagement.Worker.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LaoCai.SoftwareManagement.Worker.Services;

public class BackgroundJobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobWorker> _logger;
    private readonly string _workerId = $"worker-{Environment.MachineName}-{Guid.NewGuid().ToString("N")[..6]}";

    public BackgroundJobWorker(
        IServiceProvider serviceProvider,
        ILogger<BackgroundJobWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundJobWorker [{WorkerId}] đã khởi động.", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedCount = await ProcessPendingJobsOnceAsync(stoppingToken);
                if (processedCount == 0)
                {
                    await Task.Delay(3000, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong vòng lặp BackgroundJobWorker");
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("BackgroundJobWorker [{WorkerId}] đang dừng...", _workerId);
    }

    public async Task<int> ProcessPendingJobsOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var handlers = scope.ServiceProvider.GetServices<IJobHandler>().ToDictionary(h => h.JobType, StringComparer.OrdinalIgnoreCase);

        var nowUtc = dateTimeProvider.UtcNow;

        // Find next eligible job: Queued with NextRunAt <= now, OR Running with LeaseUntil < now
        var job = await context.BackgroundJobs
            .Where(j => (j.Status == "Queued" && j.NextRunAt <= nowUtc) ||
                        (j.Status == "Running" && j.LeaseUntil < nowUtc))
            .OrderBy(j => j.NextRunAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (job == null)
            return 0;

        // Claim job
        var leaseToken = Guid.NewGuid();
        job.Status = "Running";
        job.LeaseOwner = _workerId;
        job.LeaseToken = leaseToken;
        job.LeaseUntil = nowUtc.AddMinutes(2);
        job.Attempts += 1;

        await context.SaveChangesAsync(cancellationToken);

        try
        {
            if (handlers.TryGetValue(job.Type, out var handler))
            {
                await handler.HandleAsync(job.PayloadJson, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Không tìm thấy handler cho loại job: {JobType}", job.Type);
            }

            // Mark Succeeded
            job.Status = "Succeeded";
            job.CompletedAt = dateTimeProvider.UtcNow;
            job.LastError = null;
            job.LeaseOwner = null;
            job.LeaseToken = null;
            job.LeaseUntil = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thực thi job {JobId} ({JobType}), lần thử: {Attempts}", job.Id, job.Type, job.Attempts);

            job.LastError = ex.Message;
            job.LeaseOwner = null;
            job.LeaseToken = null;
            job.LeaseUntil = null;

            if (job.Attempts >= job.MaxAttempts)
            {
                job.Status = "Failed";
                job.CompletedAt = dateTimeProvider.UtcNow;
            }
            else
            {
                // Exponential Backoff: 1 min, 5 min, 15 min, 60 min
                var backoffMinutes = job.Attempts switch
                {
                    1 => 1,
                    2 => 5,
                    3 => 15,
                    _ => 60
                };

                job.Status = "Queued";
                job.NextRunAt = dateTimeProvider.UtcNow.AddMinutes(backoffMinutes);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return 1;
    }
}
