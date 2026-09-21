using System.Text.Json;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LaoCai.SoftwareManagement.Worker.Handlers;

public class DeploymentImportValidatePayload
{
    public Guid BatchId { get; set; }
}

public class DeploymentImportValidateJobHandler : IJobHandler
{
    private readonly IExcelService _excelService;
    private readonly ILogger<DeploymentImportValidateJobHandler> _logger;

    public string JobType => "deployment.import.validate";

    public DeploymentImportValidateJobHandler(
        IExcelService excelService,
        ILogger<DeploymentImportValidateJobHandler> logger)
    {
        _excelService = excelService;
        _logger = logger;
    }

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<DeploymentImportValidatePayload>(payloadJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (payload == null || payload.BatchId == Guid.Empty)
        {
            _logger.LogWarning("Payload không hợp lệ cho job deployment.import.validate: {Payload}", payloadJson);
            return;
        }

        _logger.LogInformation("Bắt đầu kiểm tra lô nhập liệu Excel {BatchId}...", payload.BatchId);
        var isValid = await _excelService.ValidateImportBatchAsync(payload.BatchId, cancellationToken);
        _logger.LogInformation("Kết quả kiểm tra lô nhập liệu Excel {BatchId}: {IsValid}", payload.BatchId, isValid ? "Hợp lệ (ReadyToCommit)" : "Có lỗi (FailedValidation)");
    }
}
