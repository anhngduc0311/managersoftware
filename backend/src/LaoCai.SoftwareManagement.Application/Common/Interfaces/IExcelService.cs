using LaoCai.SoftwareManagement.Application.Common.Models;

namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public record ImportRowErrorDto(
    int RowIndex,
    string? ColumnName,
    string ErrorMessage,
    string? RawValue,
    string? ErrorCode = null);

public record ImportBatchDto(
    Guid Id,
    Guid UploadedByUserId,
    string OriginalFileName,
    string StorageKey,
    string Status,
    int TotalRows,
    int ValidRows,
    int ErrorRows,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    DateTime? CommittedAt,
    string? FailureReason,
    List<ImportRowErrorDto>? Errors = null);

public record ExportRequestDto(
    Guid Id,
    Guid RequestedByUserId,
    string Status,
    string? StorageKey,
    string? FileName,
    long? FileSizeBytes,
    int? TotalRecords,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    DateTime? ExpiresAt,
    string? FailureReason);

public record ExportFilterDto(
    Guid? SoftwareId,
    Guid? OrganizationId,
    string? Status,
    string? SearchTerm);

public interface IExcelService
{
    Task<byte[]> GenerateDeploymentTemplateAsync(CancellationToken cancellationToken = default);
    Task<ImportBatchDto> UploadImportBatchAsync(Guid userId, string fileName, Stream fileStream, CancellationToken cancellationToken = default);
    Task<ImportBatchDto> GetImportBatchAsync(Guid batchId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateImportBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<int> CommitImportBatchAsync(Guid batchId, Guid userId, CancellationToken cancellationToken = default);
    Task<ExportRequestDto> RequestExportAsync(Guid userId, ExportFilterDto filter, CancellationToken cancellationToken = default);
    Task<ExportRequestDto> GetExportRequestAsync(Guid exportId, Guid userId, CancellationToken cancellationToken = default);
    Task ProcessExportAsync(Guid exportId, CancellationToken cancellationToken = default);
    Task<(Stream Stream, string FileName, string ContentType)> DownloadExportAsync(Guid exportId, Guid userId, CancellationToken cancellationToken = default);
}
