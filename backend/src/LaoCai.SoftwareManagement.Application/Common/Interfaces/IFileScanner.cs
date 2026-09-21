namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public record ScanResult(bool IsClean, string? Message = null);

public interface IFileScanner
{
    Task<ScanResult> ScanAsync(Stream content, string originalName, string contentType, CancellationToken cancellationToken = default);
}
