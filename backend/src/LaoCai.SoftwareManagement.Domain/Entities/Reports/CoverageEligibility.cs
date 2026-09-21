using LaoCai.SoftwareManagement.Domain.Common;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;

namespace LaoCai.SoftwareManagement.Domain.Entities.Reports;

public class CoverageEligibility : Entity<Guid>, IAuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid SoftwareId { get; set; }
    public Software Software { get; set; } = null!;

    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }

    public bool IsEligible { get; set; } = true;
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public void Validate()
    {
        if (OrganizationId == Guid.Empty)
            throw new InvalidOperationException("Đơn vị trong cấu hình độ phủ không được để trống.");

        if (SoftwareId == Guid.Empty)
            throw new InvalidOperationException("Phần mềm trong cấu hình độ phủ không được để trống.");

        if (ValidTo.HasValue && ValidTo.Value < ValidFrom)
            throw new InvalidOperationException("Ngày kết thúc hiệu lực không được trước ngày bắt đầu.");
    }
}
