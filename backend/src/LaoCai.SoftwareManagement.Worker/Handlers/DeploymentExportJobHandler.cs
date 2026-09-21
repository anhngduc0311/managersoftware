using System.Text.Json;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LaoCai.SoftwareManagement.Worker.Handlers;

public class DeploymentExportPayload
{
    public Guid ExportRequestId { get; set; }
}

public class DeploymentExportJobHandler : IJobHandler
{
    private readonly IExcelService _excelService;
    private readonly ILogger<DeploymentExportJobHandler> _logger;

    public string JobType => "deployment.export";

    public DeploymentExportJobHandler(
        IExcelService excelService,
        ILogger<DeploymentExportJobHandler> logger)
    {
        _excelService = excelService;
        _logger = logger;
    }

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<DeploymentExportPayload>(payloadJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (payload == null || payload.ExportRequestId == Guid.Empty)
        {
            _logger.LogWarning("Payload không hợp lệ cho job deployment.export: {Payload}", payloadJson);
            return;
        }

        _logger.LogInformation("Bắt đầu xử lý xuất dữ liệu Excel cho yêu cầu {ExportRequestId}...", payload.ExportRequestId);
        await _excelService.ProcessExportAsync(payload.ExportRequestId, cancellationToken);
        _logger.LogInformation("Hoàn tất xử lý xuất dữ liệu Excel cho yêu cầu {ExportRequestId}.", payload.ExportRequestId);
    }
}
