using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LaoCai.SoftwareManagement.Infrastructure.Services;

public class PhysicalFileStorage : IFileStorage
{
    private readonly string _baseStoragePath;

    public PhysicalFileStorage(IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:BasePath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = Path.Combine(AppContext.BaseDirectory, "app_data", "storage");
        }

        _baseStoragePath = Path.GetFullPath(configuredPath);
        Directory.CreateDirectory(Path.Combine(_baseStoragePath, "quarantine"));
        Directory.CreateDirectory(Path.Combine(_baseStoragePath, "clean"));
    }

    public async Task<string> SaveQuarantineAsync(Stream content, string originalName, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(originalName).ToLowerInvariant();
        var relativeKey = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_baseStoragePath, "quarantine", relativeKey.Replace('/', Path.DirectorySeparatorChar));

        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await content.CopyToAsync(fileStream, cancellationToken);
        return relativeKey;
    }

    public Task MoveToCleanAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var quarantinePath = Path.Combine(_baseStoragePath, "quarantine", storageKey.Replace('/', Path.DirectorySeparatorChar));
        var cleanPath = Path.Combine(_baseStoragePath, "clean", storageKey.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(quarantinePath))
        {
            throw new FileNotFoundException($"Không tìm thấy tệp trong thư mục cách ly (quarantine): {storageKey}");
        }

        var dir = Path.GetDirectoryName(cleanPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.Move(quarantinePath, cleanPath, overwrite: true);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string storageKey, bool isQuarantine, CancellationToken cancellationToken = default)
    {
        var folder = isQuarantine ? "quarantine" : "clean";
        var fullPath = Path.Combine(_baseStoragePath, folder, storageKey.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadCleanAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var cleanPath = Path.Combine(_baseStoragePath, "clean", storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(cleanPath))
        {
            throw new FileNotFoundException($"Không tìm thấy tệp an toàn: {storageKey}");
        }

        Stream stream = new FileStream(cleanPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task<Stream> OpenReadQuarantineAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var quarantinePath = Path.Combine(_baseStoragePath, "quarantine", storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(quarantinePath))
        {
            throw new FileNotFoundException($"Không tìm thấy tệp cách ly: {storageKey}");
        }

        Stream stream = new FileStream(quarantinePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }
}
