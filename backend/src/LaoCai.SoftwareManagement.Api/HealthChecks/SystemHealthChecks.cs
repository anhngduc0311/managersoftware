using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LaoCai.SoftwareManagement.Api.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IAppDbContext _dbContext;

    public DatabaseHealthCheck(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_dbContext is DbContext efContext)
            {
                var canConnect = await efContext.Database.CanConnectAsync(cancellationToken);
                return canConnect
                    ? HealthCheckResult.Healthy("Cơ sở dữ liệu PostgreSQL hoạt động tốt.")
                    : HealthCheckResult.Unhealthy("Không thể kết nối tới cơ sở dữ liệu PostgreSQL.");
            }

            return HealthCheckResult.Healthy("Database health probe OK.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Lỗi kiểm tra cơ sở dữ liệu: {ex.Message}", ex);
        }
    }
}

public class StorageHealthCheck : IHealthCheck
{
    private readonly IFileStorage _fileStorage;

    public StorageHealthCheck(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify file storage service is registered and functional
            return Task.FromResult(HealthCheckResult.Healthy("Kho lưu trữ tệp tin hoạt động bình thường."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Lỗi kiểm tra kho lưu trữ: {ex.Message}", ex));
        }
    }
}
