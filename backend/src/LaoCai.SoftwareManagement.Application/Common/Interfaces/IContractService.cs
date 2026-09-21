using LaoCai.SoftwareManagement.Application.Common.Models;

namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public record ContractDto(
    Guid Id,
    string ContractNo,
    Guid OwningOrganizationId,
    string OwningOrganizationName,
    Guid VendorId,
    string VendorName,
    string Status,
    DateOnly SignedDate,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? TotalAmount, // null if user lacks contracts.read
    string CurrencyCode,
    DateOnly? MaintenanceStartDate,
    DateOnly? MaintenanceEndDate,
    long Version,
    List<ContractItemDto> Items
);

public record ContractItemDto(
    Guid Id,
    Guid ContractId,
    Guid SoftwareId,
    string SoftwareName,
    string? Description,
    decimal? Amount, // null if user lacks contracts.read
    List<LicenseEntitlementDto> Entitlements
);

public record LicenseEntitlementDto(
    Guid Id,
    Guid ContractItemId,
    string LicenseType,
    int? Quantity,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    int AllocatedQuantity,
    int? RemainingQuantity
);

public record ContractFilter(
    string? Search = null,
    Guid? OrganizationId = null,
    Guid? VendorId = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20
);

public record CreateContractRequest(
    string ContractNo,
    Guid OwningOrganizationId,
    Guid VendorId,
    string Status,
    DateOnly SignedDate,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalAmount,
    string CurrencyCode = "VND",
    DateOnly? MaintenanceStartDate = null,
    DateOnly? MaintenanceEndDate = null
);

public record UpdateContractRequest(
    string ContractNo,
    Guid OwningOrganizationId,
    Guid VendorId,
    string Status,
    DateOnly SignedDate,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalAmount,
    string CurrencyCode = "VND",
    DateOnly? MaintenanceStartDate = null,
    DateOnly? MaintenanceEndDate = null
);

public record CreateContractItemRequest(
    Guid SoftwareId,
    string? Description,
    decimal Amount
);

public record CreateLicenseEntitlementRequest(
    string LicenseType,
    int? Quantity,
    DateOnly ValidFrom,
    DateOnly ValidTo
);

public interface IContractService
{
    Task<PagedResult<ContractDto>> GetContractsAsync(ContractFilter filter, CancellationToken cancellationToken = default);
    Task<(ContractDto Dto, string ETag)> GetContractByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ContractDto> CreateContractAsync(CreateContractRequest request, CancellationToken cancellationToken = default);
    Task<ContractDto> UpdateContractAsync(Guid id, UpdateContractRequest request, long expectedVersion, CancellationToken cancellationToken = default);
    Task<ContractItemDto> AddContractItemAsync(Guid contractId, CreateContractItemRequest request, CancellationToken cancellationToken = default);
    Task DeleteContractItemAsync(Guid contractId, Guid itemId, CancellationToken cancellationToken = default);
    Task<LicenseEntitlementDto> AddEntitlementAsync(Guid contractId, Guid itemId, CreateLicenseEntitlementRequest request, CancellationToken cancellationToken = default);
}
