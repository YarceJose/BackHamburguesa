using Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string Section = "Storage";
    /// <summary>Folder for uploads. In Docker it is a named volume mounted at /app/data.</summary>
    public string Root { get; set; } = "data";
}

/// <summary>Saves uploads on disk under random names; only relative paths go to the database.</summary>
public sealed class LocalFileStorage(IOptions<StorageOptions> options) : IFileStorage
{
    private static readonly Dictionary<string, string> ContentTypes = new()
    {
        [".jpg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp", [".pdf"] = "application/pdf"
    };

    private readonly string _root = Path.GetFullPath(options.Value.Root);

    public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct)
    {
        var relative = Path.Combine(folder, $"{DateTime.UtcNow:yyyyMM}", $"{Guid.NewGuid():N}{extension}");
        var full = Resolve(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var file = File.Create(full);
        await content.CopyToAsync(file, ct);
        return relative.Replace('\\', '/');
    }

    public Task<StoredFile?> OpenAsync(string relativePath, CancellationToken ct)
    {
        var full = Resolve(relativePath);
        if (!File.Exists(full)) return Task.FromResult<StoredFile?>(null);
        var type = ContentTypes.GetValueOrDefault(Path.GetExtension(full), "application/octet-stream");
        return Task.FromResult<StoredFile?>(new StoredFile(File.OpenRead(full), type));
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct)
    {
        var full = Resolve(relativePath);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    /// <summary>Never lets a stored path escape the storage root.</summary>
    private string Resolve(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(_root, relativePath));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage path.");
        return full;
    }
}
