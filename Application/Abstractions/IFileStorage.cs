namespace Application.Abstractions;

/// <summary>Stores uploaded files (payment receipts, QR images) outside the database.</summary>
public interface IFileStorage
{
    /// <summary>Saves the content and returns a relative path to keep in the database.</summary>
    Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct);
    Task<StoredFile?> OpenAsync(string relativePath, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
}

public sealed record StoredFile(Stream Content, string ContentType);
