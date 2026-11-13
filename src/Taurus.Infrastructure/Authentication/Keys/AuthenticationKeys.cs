using System.Security.Cryptography;
using System.Text.Json;
using Taurus.Core.Authentication.Keys;

namespace Taurus.Infrastructure.Authentication.Keys;

public sealed class AuthenticationKeys : IAuthenticationKeys, IDisposable
{
    private const int MinimumKeyBytes = 32;
    private const string Purpose = "Taurus.Csrf";

    private readonly byte[] key;
    private readonly string version;

    public AuthenticationKeys(AuthenticationKeyOptions options)
    {
        key = Convert.FromBase64String(options.Csrf.Resolve());
        version = options.Version;

        if (key.Length < MinimumKeyBytes || string.IsNullOrWhiteSpace(version))
            throw new InvalidOperationException("Invalid CSRF authentication key");
    }

    public string CsrfSeed(
        string tenantId,
        string origin,
        Guid userId,
        string sessionId)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(new[]
        {
            Purpose, version, tenantId, origin, userId.ToString(), sessionId
        });

        return Convert.ToBase64String(HMACSHA256.HashData(key, data));
    }

    public void Dispose() => CryptographicOperations.ZeroMemory(key);
}
