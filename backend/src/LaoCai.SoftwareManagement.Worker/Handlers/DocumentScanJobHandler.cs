using System.Text.Json;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LaoCai.SoftwareManagement.Worker.Handlers;

public class DocumentScanPayload
{
    public Guid DocumentId { get; set; }
}

public class DocumentScanJobHandler : IJobHandler
{
    private readonly IAppDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly IFileScanner _fileScanner;
    private readonly ILogger<DocumentScanJobHandler> _logger;

    public string JobType => "document.scan";

    public DocumentScanJobHandler(
        IAppDbContext context,
        IFileStorage fileStorage,
        IFileScanner fileScanner,
        ILogger<DocumentScanJobHandler> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _fileScanner = fileScanner;
        _logger = logger;
    }

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<DocumentScanPayload>(payloadJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (payload == null || payload.DocumentId == Guid.Empty)
        {
            _logger.LogWarning("Payload của DocumentScanJob rỗng hoặc không có DocumentId: {PayloadJson}", payloadJson);
            return;
        }

        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == payload.DocumentId, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning("Không tìm thấy tài liệu với ID: {DocumentId} để quét", payload.DocumentId);
            return;
        }

        if (document.ScanStatus == "Clean")
        {
            _logger.LogInformation("Tài liệu {DocumentId} đã được đánh dấu Clean trước đó", document.Id);
            return;
        }

        _logger.LogInformation("Bắt đầu quét an toàn tài liệu {DocumentId} ({OriginalName})", document.Id, document.OriginalName);
        document.ScanStatus = "Scanning";
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            ScanResult scanResult;
            await using (var quarantineStream = await _fileStorage.OpenReadQuarantineAsync(document.StorageKey, cancellationToken))
            {
                scanResult = await _fileScanner.ScanAsync(quarantineStream, document.OriginalName, document.ContentType, cancellationToken);
            }

            if (scanResult.IsClean)
            {
                _logger.LogInformation("Tài liệu {DocumentId} an toàn. Di chuyển vào kho lưu trữ sạch.", document.Id);
                await _fileStorage.MoveToCleanAsync(document.StorageKey, cancellationToken);
                document.ScanStatus = "Clean";
                document.CleanedAt = DateTime.UtcNow;
                document.ScanMessage = null;
            }
            else
            {
                _logger.LogWarning("Tài liệu {DocumentId} bị từ chối do vi phạm an toàn: {Message}", document.Id, scanResult.Message);
                document.ScanStatus = "Rejected";
                document.ScanMessage = scanResult.Message;

                // Send notification to uploader
                if (document.UploadedByUserId != Guid.Empty)
                {
                    _context.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        RecipientUserId = document.UploadedByUserId,
                        Type = "System",
                        Title = "Cảnh báo an toàn tệp tải lên",
                        Message = $"Tệp '{document.OriginalName}' bạn tải lên đã bị hệ thống từ chối quét an toàn: {scanResult.Message}",
                        DeduplicationKey = $"doc:rejected:{document.Id}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong quá trình quét tài liệu {DocumentId}. Đưa về trạng thái Pending để thử lại.", document.Id);
            document.ScanStatus = "Pending";
            await _context.SaveChangesAsync(cancellationToken);
            throw; // Rethrow to trigger worker retry backoff
        }
    }
}
