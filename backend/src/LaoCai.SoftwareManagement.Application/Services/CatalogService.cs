using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Services;

public class CatalogService : ICatalogService
{
    private readonly IAppDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CatalogService(IAppDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<bool> IsSoftwareCodeUniqueAsync(
        string code,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return !await _context.Software
            .AnyAsync(s => s.Code.ToUpper() == normalized && (excludeId == null || s.Id != excludeId), cancellationToken);
    }

    public async Task<bool> IsReleaseVersionUniqueAsync(
        Guid softwareId,
        string versionName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = versionName.Trim().ToUpperInvariant();
        return !await _context.SoftwareReleases
            .AnyAsync(r => r.SoftwareId == softwareId &&
                           r.VersionName.ToUpper() == normalized &&
                           (excludeId == null || r.Id != excludeId), cancellationToken);
    }

    public async Task<Software> AcceptProposalAsync(
        Guid proposalId,
        Guid reviewerUserId,
        Guid? existingSoftwareId,
        string? newSoftwareCode,
        Guid? categoryId,
        Guid? vendorId,
        CancellationToken cancellationToken = default)
    {
        var proposal = await _context.CatalogProposals
            .FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken);

        if (proposal == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đề xuất danh mục có ID: {proposalId}");
        }

        if (proposal.Status != "Pending")
        {
            throw new InvalidOperationException($"Đề xuất đang ở trạng thái '{proposal.Status}', không thể duyệt.");
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        Software software;

        if (existingSoftwareId.HasValue)
        {
            var existing = await _context.Software
                .FirstOrDefaultAsync(s => s.Id == existingSoftwareId.Value, cancellationToken);

            if (existing == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy phần mềm có ID: {existingSoftwareId.Value}");
            }

            software = existing;
            proposal.CreatedSoftwareId = software.Id;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(newSoftwareCode) || !categoryId.HasValue || !vendorId.HasValue)
            {
                throw new ArgumentException("Mã phần mềm, nhóm phần mềm và nhà cung cấp là bắt buộc khi tạo mới phần mềm từ đề xuất.");
            }

            var isUnique = await IsSoftwareCodeUniqueAsync(newSoftwareCode, null, cancellationToken);
            if (!isUnique)
            {
                throw new InvalidOperationException($"Mã phần mềm '{newSoftwareCode}' đã tồn tại trong hệ thống.");
            }

            software = new Software
            {
                Id = Guid.NewGuid(),
                Code = newSoftwareCode.Trim().ToUpperInvariant(),
                Name = proposal.SoftwareName.Trim(),
                CategoryId = categoryId.Value,
                VendorId = vendorId.Value,
                Description = proposal.Description,
                LifecycleStatus = "Active",
                Version = 1,
                CreatedAt = nowUtc,
                CreatedBy = reviewerUserId
            };

            _context.Software.Add(software);
            proposal.CreatedSoftwareId = software.Id;
        }

        proposal.Status = "Accepted";
        proposal.ReviewedByUserId = reviewerUserId;
        proposal.ReviewedAt = nowUtc;
        proposal.UpdatedAt = nowUtc;
        proposal.UpdatedBy = reviewerUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return software;
    }

    public async Task RejectProposalAsync(
        Guid proposalId,
        Guid reviewerUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Lý do từ chối là bắt buộc.");
        }

        var proposal = await _context.CatalogProposals
            .FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken);

        if (proposal == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đề xuất danh mục có ID: {proposalId}");
        }

        if (proposal.Status != "Pending")
        {
            throw new InvalidOperationException($"Đề xuất đang ở trạng thái '{proposal.Status}', không thể từ chối.");
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        proposal.Status = "Rejected";
        proposal.RejectionReason = reason.Trim();
        proposal.ReviewedByUserId = reviewerUserId;
        proposal.ReviewedAt = nowUtc;
        proposal.UpdatedAt = nowUtc;
        proposal.UpdatedBy = reviewerUserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
