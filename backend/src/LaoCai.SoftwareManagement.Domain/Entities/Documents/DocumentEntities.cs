using System.ComponentModel.DataAnnotations;
using LaoCai.SoftwareManagement.Domain.Common;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;

namespace LaoCai.SoftwareManagement.Domain.Entities.Documents;

public class Document : Entity<Guid>
{
    [Required]
    [MaxLength(255)]
    public string StorageKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    [Required]
    [MaxLength(64)]
    public string ChecksumSha256 { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string ScanStatus { get; set; } = "Pending"; // Pending | Scanning | Clean | Rejected

    public string? ScanMessage { get; set; }

    public Guid UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;

    public bool IsOrphaned { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CleanedAt { get; set; }

    public ICollection<DocumentAttachment> Attachments { get; set; } = new List<DocumentAttachment>();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(StorageKey))
            throw new ValidationException("Khóa lưu trữ tệp (StorageKey) không được để trống.");

        if (string.IsNullOrWhiteSpace(OriginalName))
            throw new ValidationException("Tên tệp gốc không được để trống.");

        if (SizeBytes <= 0)
            throw new ValidationException("Dung lượng tệp phải lớn hơn 0.");

        if (SizeBytes > 20 * 1024 * 1024) // 20 MB limit
            throw new ValidationException("Dung lượng tệp vượt quá giới hạn cho phép (tối đa 20 MB).");

        if (string.IsNullOrWhiteSpace(ChecksumSha256))
            throw new ValidationException("Mã băm kiểm tra (SHA-256) không được để trống.");
    }
}

public class DocumentAttachment : Entity<Guid>
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty; // Contract | DeploymentRevision

    public Guid EntityId { get; set; }

    public Guid AttachedByUserId { get; set; }
    public User AttachedByUser { get; set; } = null!;

    public DateTime AttachedAt { get; set; } = DateTime.UtcNow;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(EntityType))
            throw new ValidationException("Loại thực thể đính kèm không được để trống.");

        if (EntityId == Guid.Empty)
            throw new ValidationException("Định danh thực thể đính kèm không hợp lệ.");
    }
}
