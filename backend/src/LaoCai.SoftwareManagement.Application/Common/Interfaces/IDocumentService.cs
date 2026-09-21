namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public record DocumentDto(
    Guid Id,
    string OriginalName,
    string ContentType,
    long SizeBytes,
    string ChecksumSha256,
    string ScanStatus,
    string? ScanMessage,
    Guid UploadedByUserId,
    string UploadedByUserName,
    DateTime CreatedAt
);

public record DocumentAttachmentDto(
    Guid AttachmentId,
    Guid DocumentId,
    string OriginalName,
    string ContentType,
    long SizeBytes,
    string ScanStatus,
    string? ScanMessage,
    string EntityType,
    Guid EntityId,
    Guid AttachedByUserId,
    string AttachedByUserName,
    DateTime AttachedAt
);

public record UploadDocumentCommand(
    Stream Content,
    string OriginalName,
    string ContentType,
    long SizeBytes,
    string EntityType, // Contract | DeploymentRevision
    Guid EntityId
);

public interface IDocumentService
{
    Task<DocumentAttachmentDto> UploadAndAttachAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default);
    Task<List<DocumentAttachmentDto>> GetAttachmentsAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<(Stream FileStream, string ContentType, string OriginalName)> DownloadDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task UnlinkAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default);
}
