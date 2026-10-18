namespace Krizovka.Api.Reports;

public interface IAttachmentStorage
{
    Task<string> SaveAttachmentAsync(Guid reportId, Guid attachmentId, string fileName, Stream fileStream, CancellationToken ct = default);
    Task<Stream?> OpenAttachmentReadStreamAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAttachmentAsync(string storageKey, CancellationToken ct = default);
}

public sealed class FileSystemAttachmentStorage : IAttachmentStorage
{
    private readonly string _baseDirectory;

    public FileSystemAttachmentStorage(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPath = configuration["Storage:AttachmentsDirectory"];
        var primaryPath = !string.IsNullOrWhiteSpace(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.Combine(environment.ContentRootPath, "data", "attachments");

        try
        {
            Directory.CreateDirectory(primaryPath);
            _baseDirectory = primaryPath;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            var fallbackPath = Path.Combine(Path.GetTempPath(), "krizovka", "attachments");
            Directory.CreateDirectory(fallbackPath);
            _baseDirectory = fallbackPath;
        }
    }

    public async Task<string> SaveAttachmentAsync(Guid reportId, Guid attachmentId, string fileName, Stream fileStream, CancellationToken ct = default)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "attachment.bin";
        }

        var reportDir = Path.Combine(_baseDirectory, reportId.ToString());
        Directory.CreateDirectory(reportDir);

        var fileTargetName = $"{attachmentId}_{safeFileName}";
        var fullPath = Path.Combine(reportDir, fileTargetName);

        await using var destination = File.Create(fullPath);
        await fileStream.CopyToAsync(destination, ct);

        return $"{reportId}/{fileTargetName}";
    }

    public Task<Stream?> OpenAttachmentReadStreamAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return Task.FromResult<Stream?>(null);

        var fullPath = Path.GetFullPath(Path.Combine(_baseDirectory, storageKey));
        if (!fullPath.StartsWith(_baseDirectory, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<Stream?>(null);

        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        var stream = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAttachmentAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return Task.CompletedTask;

        var fullPath = Path.GetFullPath(Path.Combine(_baseDirectory, storageKey));
        if (fullPath.StartsWith(_baseDirectory, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
