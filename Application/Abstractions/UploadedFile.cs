using Application.Exceptions;
using Domain.Errors;

namespace Application.Abstractions;

/// <summary>A file coming from a multipart request.</summary>
public sealed record UploadedFile(Stream Content, long Length);

public static class UploadRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Accepts JPG, PNG, WEBP or PDF by their real signature (not by the name the client sends)
    /// and returns a buffered copy plus the extension to store.
    /// </summary>
    public static async Task<(MemoryStream Content, string Extension)> ReadValidatedAsync(
        UploadedFile file, bool allowPdf, CancellationToken ct, string errorCode = ErrorCodes.ReceiptInvalid)
    {
        if (file.Length is <= 0 or > MaxBytes)
            throw new BusinessRuleException(errorCode, "The file must be between 1 byte and 5 MB.");

        var buffer = new MemoryStream();
        await file.Content.CopyToAsync(buffer, ct);
        if (buffer.Length > MaxBytes)
            throw new BusinessRuleException(errorCode, "The file must be at most 5 MB.");

        var bytes = buffer.GetBuffer();
        string? extension =
            Starts(bytes, 0xFF, 0xD8, 0xFF) ? ".jpg" :
            Starts(bytes, 0x89, 0x50, 0x4E, 0x47) ? ".png" :
            buffer.Length > 12 && Starts(bytes, 0x52, 0x49, 0x46, 0x46) && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50 ? ".webp" :
            allowPdf && Starts(bytes, 0x25, 0x50, 0x44, 0x46) ? ".pdf" :
            null;

        if (extension is null)
            throw new BusinessRuleException(errorCode, allowPdf ? "Upload a JPG, PNG, WEBP or PDF file." : "Upload a JPG, PNG or WEBP image.");

        buffer.Position = 0;
        return (buffer, extension);
    }

    private static bool Starts(byte[] bytes, params byte[] signature) =>
        bytes.Length >= signature.Length && signature.Select((b, i) => bytes[i] == b).All(x => x);
}
