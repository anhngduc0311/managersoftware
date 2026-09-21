using System.ComponentModel.DataAnnotations;
using LaoCai.SoftwareManagement.Domain.Common;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using LaoCai.SoftwareManagement.Domain.Entities.Deployments;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;

namespace LaoCai.SoftwareManagement.Domain.Entities.Contracts;

public class Contract : Entity<Guid>, IAuditableEntity, IVersionedEntity
{
    [Required]
    [MaxLength(100)]
    public string ContractNo { get; set; } = string.Empty;

    public Guid OwningOrganizationId { get; set; }
    public Organization OwningOrganization { get; set; } = null!;

    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Active"; // Draft | Active | Closed | Cancelled

    public DateOnly SignedDate { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "VND";

    public DateOnly? MaintenanceStartDate { get; set; }
    public DateOnly? MaintenanceEndDate { get; set; }

    public long Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<ContractItem> Items { get; set; } = new List<ContractItem>();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ContractNo))
            throw new ValidationException("Số hợp đồng không được để trống.");

        if (TotalAmount < 0)
            throw new ValidationException("Tổng giá trị hợp đồng không được âm.");

        if (EndDate < StartDate)
            throw new ValidationException("Ngày kết thúc hợp đồng không được trước ngày bắt đầu.");

        if (MaintenanceStartDate.HasValue && MaintenanceEndDate.HasValue && MaintenanceEndDate < MaintenanceStartDate)
            throw new ValidationException("Ngày kết thúc bảo trì không được trước ngày bắt đầu bảo trì.");
    }
}

public class ContractItem : Entity<Guid>
{
    public Guid ContractId { get; set; }
    public Contract Contract { get; set; } = null!;

    public Guid SoftwareId { get; set; }
    public Software Software { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public decimal Amount { get; set; }

    public ICollection<LicenseEntitlement> Entitlements { get; set; } = new List<LicenseEntitlement>();

    public void Validate()
    {
        if (Amount < 0)
            throw new ValidationException("Giá trị hạng mục hợp đồng không được âm.");
    }
}

public class LicenseEntitlement : Entity<Guid>
{
    public Guid ContractItemId { get; set; }
    public ContractItem ContractItem { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string LicenseType { get; set; } = "Seat"; // Seat | Unlimited

    public int? Quantity { get; set; } // Required if Seat, null if Unlimited

    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }

    public ICollection<LicenseAllocation> Allocations { get; set; } = new List<LicenseAllocation>();

    public void Validate()
    {
        if (LicenseType.Equals("Seat", StringComparison.OrdinalIgnoreCase))
        {
            if (!Quantity.HasValue || Quantity.Value < 0)
                throw new ValidationException("Loại license Seat bắt buộc phải có số lượng lớn hơn hoặc bằng 0.");
        }
        else if (LicenseType.Equals("Unlimited", StringComparison.OrdinalIgnoreCase))
        {
            Quantity = null;
        }
        else
        {
            throw new ValidationException("Loại license không hợp lệ. Chỉ chấp nhận 'Seat' hoặc 'Unlimited'.");
        }

        if (ValidTo < ValidFrom)
            throw new ValidationException("Ngày hết hạn license không được trước ngày bắt đầu hiệu lực.");
    }
}

public class LicenseAllocation : Entity<Guid>
{
    public Guid EntitlementId { get; set; }
    public LicenseEntitlement Entitlement { get; set; } = null!;

    public Guid DeploymentId { get; set; }
    public Deployment Deployment { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime AllocatedAt { get; set; } = DateTime.UtcNow;

    public void Validate()
    {
        if (Quantity <= 0)
            throw new ValidationException("Số lượng license phân bổ phải lớn hơn 0.");
    }
}
