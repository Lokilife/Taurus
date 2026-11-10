using System.Security.Cryptography;

namespace Taurus.Application.Features.Sessions;

public static class SessionTokens
{
    private const int TokenBytes = 32;
    private const int MaximumTokenBytes = 512;

    public const int MaximumTokenCharacters = 128;

    public static string Create()
    {
        Span<byte> bytes = stackalloc byte[TokenBytes];

        RandomNumberGenerator.Fill(bytes);

        return Convert.ToHexString(bytes);
    }

    public static string Digest(string token)
    {
        if (token.Length > MaximumTokenCharacters)
            throw new ArgumentException("Session token is too long", nameof(token));

        Span<byte> bytes = stackalloc byte[MaximumTokenBytes];
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        var written = System.Text.Encoding.UTF8.GetBytes(token.AsSpan(), bytes);

        SHA256.HashData(bytes[..written], digest);
        CryptographicOperations.ZeroMemory(bytes);

        return Convert.ToHexString(digest);
    }
}
