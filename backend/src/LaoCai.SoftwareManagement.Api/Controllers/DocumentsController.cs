using LaoCai.SoftwareManagement.Application.Common.Exceptions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LaoCai.SoftwareManagement.Api.Controllers;

[ApiController]
[Route("api/v1/documents")]
public class DocumentsController : BaseApiController
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(
        [FromForm] IFormFile file,
        [FromForm] string entityType,
        [FromForm] Guid entityId,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            throw new CustomValidationException("file", "Vui lòng chọn tệp để tải lên.");
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadDocumentCommand(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            entityType,
            entityId);

        var result = await _documentService.UploadAndAttachAsync(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("entity/{entityType}/{entityId:guid}")]
    public async Task<IActionResult> GetAttachments(string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        var result = await _documentService.GetAttachmentsAsync(entityType, entityId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id, CancellationToken cancellationToken)
    {
        var (fileStream, contentType, originalName) = await _documentService.DownloadDocumentAsync(id, cancellationToken);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(fileStream, contentType, originalName);
    }

    [HttpDelete("attachments/{attachmentId:guid}")]
    public async Task<IActionResult> UnlinkAttachment(Guid attachmentId, CancellationToken cancellationToken)
    {
        await _documentService.UnlinkAttachmentAsync(attachmentId, cancellationToken);
        return NoContent();
    }
}
