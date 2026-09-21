using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[ApiController]
[Route("api/v1/deployments/excel")]
public class ExcelController : BaseApiController
{
    private readonly IExcelService _excelService;
    private readonly ICurrentUserService _currentUserService;

    public ExcelController(
        IExcelService excelService,
        ICurrentUserService currentUserService)
    {
        _excelService = excelService;
        _currentUserService = currentUserService;
    }

    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate(CancellationToken cancellationToken = default)
    {
        var fileBytes = await _excelService.GenerateDeploymentTemplateAsync(cancellationToken);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_Nhap_Lieu_Trien_Khai.xlsx");
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImportBatch(
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new CustomValidationException("file", "Vui lòng chọn tệp Excel hợp lệ để tải lên.");
        }

        var userId = _currentUserService.UserId ?? Guid.Empty;
        await using var stream = file.OpenReadStream();
        var batch = await _excelService.UploadImportBatchAsync(userId, file.FileName, stream, cancellationToken);
        return Ok(batch);
    }

    [HttpGet("import/{batchId:guid}")]
    public async Task<IActionResult> GetImportBatch(
        [FromRoute] Guid batchId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var batch = await _excelService.GetImportBatchAsync(batchId, userId, cancellationToken);
        return Ok(batch);
    }

    [HttpPost("import/{batchId:guid}/validate")]
    public async Task<IActionResult> ValidateImportBatch(
        [FromRoute] Guid batchId,
        CancellationToken cancellationToken = default)
    {
        var isValid = await _excelService.ValidateImportBatchAsync(batchId, cancellationToken);
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var batch = await _excelService.GetImportBatchAsync(batchId, userId, cancellationToken);
        return Ok(new { IsValid = isValid, Batch = batch });
    }

    [HttpPost("import/{batchId:guid}/commit")]
    public async Task<IActionResult> CommitImportBatch(
        [FromRoute] Guid batchId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var committedCount = await _excelService.CommitImportBatchAsync(batchId, userId, cancellationToken);
        return Ok(new { Success = true, CommittedCount = committedCount, Message = $"Đã tạo thành công {committedCount} hồ sơ triển khai (Bản nháp)." });
    }

    [HttpPost("export")]
    public async Task<IActionResult> RequestExport(
        [FromBody] ExportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var request = await _excelService.RequestExportAsync(userId, filter, cancellationToken);
        return Ok(request);
    }

    [HttpGet("export/{exportId:guid}")]
    public async Task<IActionResult> GetExportStatus(
        [FromRoute] Guid exportId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var request = await _excelService.GetExportRequestAsync(exportId, userId, cancellationToken);
        return Ok(request);
    }

    [HttpGet("export/{exportId:guid}/download")]
    public async Task<IActionResult> DownloadExport(
        [FromRoute] Guid exportId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var (stream, fileName, contentType) = await _excelService.DownloadExportAsync(exportId, userId, cancellationToken);
        return File(stream, contentType, fileName);
    }
}
