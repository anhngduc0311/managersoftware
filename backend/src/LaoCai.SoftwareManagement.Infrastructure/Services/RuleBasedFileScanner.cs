using System.IO.Compression;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;

namespace LaoCai.SoftwareManagement.Infrastructure.Services;

public class RuleBasedFileScanner : IFileScanner
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg"
    };

    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    public async Task<ScanResult> ScanAsync(Stream content, string originalName, string contentType, CancellationToken cancellationToken = default)
    {
        // 1. Check extension
        var ext = Path.GetExtension(originalName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            return new ScanResult(false, $"Định dạng tệp '{ext}' không được phép. Chỉ chấp nhận: PDF, DOCX, XLSX, PNG, JPEG.");
        }

        // 2. Check length
        if (content.CanSeek)
        {
            content.Position = 0;
            if (content.Length > MaxFileSizeBytes)
            {
                return new ScanResult(false, $"Dung lượng tệp ({content.Length} bytes) vượt quá giới hạn cho phép (tối đa 20 MB).");
            }
            if (content.Length == 0)
            {
                return new ScanResult(false, "Tệp rỗng không có nội dung.");
            }
        }

        // 3. Read header bytes (first 16 bytes)
        var header = new byte[16];
        if (content.CanSeek) content.Position = 0;
        var bytesRead = await content.ReadAsync(header.AsMemory(0, 16), cancellationToken);

        if (bytesRead < 4)
        {
            return new ScanResult(false, "Tệp không hợp lệ hoặc kích thước quá nhỏ.");
        }

        // Check for Executable signatures: Windows MZ (4D 5A) or Linux ELF (7F 45 4C 46)
        if (header[0] == 0x4D && header[1] == 0x5A)
        {
            return new ScanResult(false, "Phát hiện tệp thực thi Windows (PE/MZ Header) bị cấm vì lý do an ninh.");
        }

        if (header[0] == 0x7F && header[1] == 0x45 && header[2] == 0x4C && header[3] == 0x46)
        {
            return new ScanResult(false, "Phát hiện tệp thực thi Linux (ELF Header) bị cấm vì lý do an ninh.");
        }

        var normalizedExt = ext.ToLowerInvariant();

        // Check format signatures
        switch (normalizedExt)
        {
            case ".pdf":
                // %PDF- => 0x25, 0x50, 0x44, 0x46
                if (header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46)
                {
                    return new ScanResult(false, "Chữ ký tệp không khớp với định dạng chuẩn PDF (%PDF-).");
                }
                break;

            case ".png":
                // 89 50 4E 47 0D 0A 1A 0A
                if (bytesRead < 8 ||
                    header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47 ||
                    header[4] != 0x0D || header[5] != 0x0A || header[6] != 0x1A || header[7] != 0x0A)
                {
                    return new ScanResult(false, "Chữ ký tệp không khớp với định dạng chuẩn PNG.");
                }
                break;

            case ".jpg":
            case ".jpeg":
                // FF D8 FF
                if (header[0] != 0xFF || header[1] != 0xD8 || header[2] != 0xFF)
                {
                    return new ScanResult(false, "Chữ ký tệp không khớp với định dạng chuẩn JPEG.");
                }
                break;

            case ".docx":
            case ".xlsx":
                // Zip signature: 50 4B 03 04
                if (header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
                {
                    return new ScanResult(false, $"Chữ ký tệp không khớp với định dạng nén chuẩn Office OpenXML ({normalizedExt}).");
                }

                // Inspect zip package for Zip-bomb or embedded malicious scripts
                try
                {
                    if (content.CanSeek) content.Position = 0;
                    using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);

                    long totalUncompressedBytes = 0;
                    foreach (var entry in archive.Entries)
                    {
                        var entryExt = Path.GetExtension(entry.FullName).ToLowerInvariant();
                        if (entryExt is ".exe" or ".dll" or ".bat" or ".cmd" or ".vbs" or ".ps1" or ".js" or ".scr")
                        {
                            return new ScanResult(false, $"Tệp nén chứa thành phần thực thi bị cấm: '{entry.FullName}'.");
                        }

                        totalUncompressedBytes += entry.Length;
                        if (totalUncompressedBytes > 200 * 1024 * 1024) // 200 MB max uncompressed
                        {
                            return new ScanResult(false, "Phát hiện nguy cơ tấn công từ chối dịch vụ giải nén (Zip Bomb).");
                        }
                    }
                }
                catch (InvalidDataException)
                {
                    return new ScanResult(false, "Cấu trúc gói Office OpenXML bị hỏng hoặc không đúng chuẩn.");
                }
                break;
        }

        return new ScanResult(true);
    }
}
