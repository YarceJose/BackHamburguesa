using System.Security.Cryptography;

namespace Application.Features.Orders;

/// <summary>Short, readable public order code (no 0/O/1/I), e.g. "K7P2QX".</summary>
public static class OrderCode
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string New(int length = 6) =>
        string.Create(length, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++) span[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        });
}
