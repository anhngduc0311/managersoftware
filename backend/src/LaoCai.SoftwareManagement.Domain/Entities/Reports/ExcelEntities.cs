using LaoCai.SoftwareManagement.Domain.Common;

namespace LaoCai.SoftwareManagement.Domain.Entities.Reports;

public class ImportBatch : Entity<Guid>, IAuditableEntity
{
    public Guid DocumentId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Validating, Validated, FailedValidation, Committing, Committed, FailedCommit
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    public string? StagingDataJson { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime? ValidatedAt { get; set; }
    public DateTime? CommittedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<ImportRowError> RowErrors { get; set; } = new List<ImportRowError>();

    public void Validate()
    {
        if (DocumentId == Guid.Empty)
            throw new InvalidOperationException("Tệp nguồn nhập dữ liệu không được để trống.");

        if (RequestedByUserId == Guid.Empty)
            throw new InvalidOperationException("Người yêu cầu nhập dữ liệu không được để trống.");

        if (TotalRows > 5000)
            throw new InvalidOperationException("Số lượng dòng trong tệp nhập vượt quá giới hạn tối đa cho phép (5.000 dòng).");
    }
}

public class ImportRowError : Entity<Guid>
{
    public Guid BatchId { get; set; }
    public ImportBatch Batch { get; set; } = null!;

    public int RowIndex { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? RawValue { get; set; }
}

public class ExportRequest : Entity<Guid>
{
    public string ExportType { get; set; } = "Deployments"; // Deployments, Contracts, Dashboard
    public string FilterSnapshotJson { get; set; } = "{}";
    public string Status { get; set; } = "Queued"; // Queued, Processing, Completed, Failed
    public Guid? DocumentId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ExportType))
            throw new InvalidOperationException("Loại xuất dữ liệu không được để trống.");

        if (RequestedByUserId == Guid.Empty)
            throw new InvalidOperationException("Người yêu cầu xuất dữ liệu không được để trống.");

        if (ExpiresAt <= CreatedAt)
            throw new InvalidOperationException("Thời hạn tệp xuất (TTL) phải lớn hơn thời điểm tạo.");
    }
}
