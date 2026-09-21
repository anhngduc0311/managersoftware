namespace LaoCai.SoftwareManagement.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<string> SaveQuarantineAsync(Stream content, string originalName, CancellationToken cancellationToken = default);
    Task MoveToCleanAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, bool isQuarantine, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadCleanAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadQuarantineAsync(string storageKey, CancellationToken cancellationToken = default);
}
