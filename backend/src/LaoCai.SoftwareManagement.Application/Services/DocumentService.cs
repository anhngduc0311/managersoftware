using System.Security.Cryptography;
using System.Text.Json;
using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IAppDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly IBackgroundJobService _jobService;
    private readonly IScopeAuthorizationService _scopeAuth;
    private readonly ICurrentUserService _currentUser;

    public DocumentService(
        IAppDbContext context,
        IFileStorage fileStorage,
        IBackgroundJobService jobService,
        IScopeAuthorizationService scopeAuth,
        ICurrentUserService currentUser)
    {
        _context = context;
        _fileStorage = fileStorage;
        _jobService = jobService;
        _scopeAuth = scopeAuth;
        _currentUser = currentUser;
    }

    public async Task<DocumentAttachmentDto> UploadAndAttachAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUser.UserId ?? Guid.Empty;

        // 1. Check parent entity existence and permissions
        Guid owningOrgId;
        if (command.EntityType.Equals("Contract", StringComparison.OrdinalIgnoreCase))
        {
            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == command.EntityId, cancellationToken);
            if (contract == null)
                throw new NotFoundException("Hợp đồng", command.EntityId);

            owningOrgId = contract.OwningOrganizationId;
            if (_currentUser.UserId.HasValue)
            {
                var isAllowed = await _scopeAuth.HasPermissionAsync(currentUserId, "contracts.write", owningOrgId, cancellationToken);
                if (!isAllowed)
                    throw new ForbiddenException("Bạn không có quyền cập nhật tài liệu cho hợp đồng này.");
            }

            contract.Version += 1;
        }
        else if (command.EntityType.Equals("DeploymentRevision", StringComparison.OrdinalIgnoreCase))
        {
            var revision = await _context.DeploymentRevisions
                .Include(r => r.Deployment)
                .FirstOrDefaultAsync(r => r.Id == command.EntityId, cancellationToken);

            if (revision == null)
                throw new NotFoundException("Phiên bản hồ sơ triển khai", command.EntityId);

            if (!revision.WorkflowStatus.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            {
                throw new CustomValidationException("workflowStatus", "Chỉ được đính kèm tài liệu khi hồ sơ ở trạng thái Nháp (Draft).");
            }

            owningOrgId = revision.Deployment.OrganizationId;
            if (_currentUser.UserId.HasValue)
            {
                var isAllowed = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.write", owningOrgId, cancellationToken);
                if (!isAllowed)
                    throw new ForbiddenException("Bạn không có quyền đính kèm tài liệu cho hồ sơ triển khai này.");
            }

            revision.Version += 1;
        }
        else
        {
            throw new CustomValidationException("entityType", "Loại thực thể đính kèm không hợp lệ. Chỉ chấp nhận 'Contract' hoặc 'DeploymentRevision'.");
        }

        // 2. Validate file size and extension
        if (command.SizeBytes <= 0 || command.SizeBytes > 20 * 1024 * 1024)
        {
            throw new CustomValidationException("file", "Dung lượng tệp không hợp lệ (phải lớn hơn 0 và tối đa 20 MB).");
        }

        var ext = Path.GetExtension(command.OriginalName).ToLowerInvariant();
        var allowedExts = new HashSet<string> { ".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg" };
        if (!allowedExts.Contains(ext))
        {
            throw new CustomValidationException("file", $"Định dạng tệp '{ext}' không được chấp nhận. Chỉ cho phép: PDF, DOCX, XLSX, PNG, JPEG.");
        }

        // 3. Compute Checksum SHA-256
        if (command.Content.CanSeek) command.Content.Position = 0;
        using var sha = SHA256.Create();
        var hashBytes = await sha.ComputeHashAsync(command.Content, cancellationToken);
        var checksumHex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        // 4. Save to Quarantine storage
        if (command.Content.CanSeek) command.Content.Position = 0;
        var storageKey = await _fileStorage.SaveQuarantineAsync(command.Content, command.OriginalName, cancellationToken);

        // 5. Create Document entity
        var document = new Document
        {
            Id = Guid.NewGuid(),
            StorageKey = storageKey,
            OriginalName = command.OriginalName,
            ContentType = string.IsNullOrWhiteSpace(command.ContentType) ? "application/octet-stream" : command.ContentType,
            SizeBytes = command.SizeBytes,
            ChecksumSha256 = checksumHex,
            ScanStatus = "Pending",
            UploadedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        document.Validate();

        var attachment = new DocumentAttachment
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            EntityType = command.EntityType,
            EntityId = command.EntityId,
            AttachedByUserId = currentUserId,
            AttachedAt = DateTime.UtcNow
        };

        attachment.Validate();

        _context.Documents.Add(document);
        _context.DocumentAttachments.Add(attachment);

        // 6. Enqueue scan job
        await _jobService.EnqueueAsync("document.scan", new { documentId = document.Id }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var uploader = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        return new DocumentAttachmentDto(
            attachment.Id,
            document.Id,
            document.OriginalName,
            document.ContentType,
            document.SizeBytes,
            document.ScanStatus,
            document.ScanMessage,
            attachment.EntityType,
            attachment.EntityId,
            currentUserId,
            uploader?.DisplayName ?? uploader?.UserName ?? "Người dùng",
            attachment.AttachedAt);
    }

    public async Task<List<DocumentAttachmentDto>> GetAttachmentsAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var attachments = await _context.DocumentAttachments
            .AsNoTracking()
            .Include(a => a.Document)
            .Include(a => a.AttachedByUser)
            .Where(a => a.EntityType.ToLower() == entityType.ToLower() && a.EntityId == entityId)
            .OrderByDescending(a => a.AttachedAt)
            .ToListAsync(cancellationToken);

        return attachments.Select(a => new DocumentAttachmentDto(
            a.Id,
            a.DocumentId,
            a.Document.OriginalName,
            a.Document.ContentType,
            a.Document.SizeBytes,
            a.Document.ScanStatus,
            a.Document.ScanMessage,
            a.EntityType,
            a.EntityId,
            a.AttachedByUserId,
            a.AttachedByUser?.DisplayName ?? a.AttachedByUser?.UserName ?? "Người dùng",
            a.AttachedAt)).ToList();
    }

    public async Task<(Stream FileStream, string ContentType, string OriginalName)> DownloadDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .Include(d => d.Attachments)
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document == null)
            throw new NotFoundException("Tài liệu", documentId);

        if (!document.ScanStatus.Equals("Clean", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException($"Tệp chưa qua kiểm tra an toàn hoặc đã bị từ chối (Trạng thái hiện tại: {document.ScanStatus}).");
        }

        // Check read permission on any attached entity
        if (_currentUser.UserId.HasValue)
        {
            bool hasAccess = false;
            foreach (var att in document.Attachments)
            {
                if (att.EntityType.Equals("Contract", StringComparison.OrdinalIgnoreCase))
                {
                    var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == att.EntityId, cancellationToken);
                    if (contract != null && await _scopeAuth.HasPermissionAsync(_currentUser.UserId.Value, "contracts.read", contract.OwningOrganizationId, cancellationToken))
                    {
                        hasAccess = true;
                        break;
                    }
                }
                else if (att.EntityType.Equals("DeploymentRevision", StringComparison.OrdinalIgnoreCase))
                {
                    var rev = await _context.DeploymentRevisions.Include(r => r.Deployment).FirstOrDefaultAsync(r => r.Id == att.EntityId, cancellationToken);
                    if (rev != null && await _scopeAuth.HasPermissionAsync(_currentUser.UserId.Value, "deployments.read", rev.Deployment.OrganizationId, cancellationToken))
                    {
                        hasAccess = true;
                        break;
                    }
                }
            }

            if (!hasAccess && document.Attachments.Count > 0)
            {
                throw new ForbiddenException("Bạn không có quyền tải xuống tài liệu này.");
            }
        }

        var stream = await _fileStorage.OpenReadCleanAsync(document.StorageKey, cancellationToken);
        return (stream, document.ContentType, document.OriginalName);
    }

    public async Task UnlinkAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await _context.DocumentAttachments
            .Include(a => a.Document)
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);

        if (attachment == null)
            throw new NotFoundException("Tài liệu đính kèm", attachmentId);

        var currentUserId = _currentUser.UserId ?? Guid.Empty;

        // Verify write permission
        if (attachment.EntityType.Equals("Contract", StringComparison.OrdinalIgnoreCase))
        {
            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == attachment.EntityId, cancellationToken);
            if (contract != null)
            {
                if (_currentUser.UserId.HasValue)
                {
                    var isAllowed = await _scopeAuth.HasPermissionAsync(currentUserId, "contracts.write", contract.OwningOrganizationId, cancellationToken);
                    if (!isAllowed)
                        throw new ForbiddenException("Bạn không có quyền xóa tài liệu của hợp đồng này.");
                }
                contract.Version += 1;
            }
        }
        else if (attachment.EntityType.Equals("DeploymentRevision", StringComparison.OrdinalIgnoreCase))
        {
            var rev = await _context.DeploymentRevisions.Include(r => r.Deployment).FirstOrDefaultAsync(r => r.Id == attachment.EntityId, cancellationToken);
            if (rev != null)
            {
                if (!rev.WorkflowStatus.Equals("Draft", StringComparison.OrdinalIgnoreCase))
                {
                    throw new CustomValidationException("workflowStatus", "Chỉ được gỡ tài liệu khi hồ sơ ở trạng thái Nháp (Draft).");
                }

                if (_currentUser.UserId.HasValue)
                {
                    var isAllowed = await _scopeAuth.HasPermissionAsync(currentUserId, "deployments.write", rev.Deployment.OrganizationId, cancellationToken);
                    if (!isAllowed)
                        throw new ForbiddenException("Bạn không có quyền gỡ tài liệu của hồ sơ triển khai này.");
                }
                rev.Version += 1;
            }
        }

        _context.DocumentAttachments.Remove(attachment);

        // Check if document has remaining links
        var otherLinksCount = await _context.DocumentAttachments.CountAsync(a => a.DocumentId == attachment.DocumentId && a.Id != attachmentId, cancellationToken);
        if (otherLinksCount == 0)
        {
            attachment.Document.IsOrphaned = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
