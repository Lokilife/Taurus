using System.Security.Cryptography;
using Grpc.Core;
using Taurus.Application.Features.Sessions;

namespace Taurus.Server.Security;

public static class BrowserSecurity
{
    public const string SessionCookie = "__Host-TaurusSession";
    public const string DevelopmentSessionCookie = "TaurusDevelopmentSession";
    public const string DevelopmentCsrfCookie = "TaurusDevelopmentCsrf";
    public const string CsrfCookie = "__Host-TaurusCsrf";
    public const string CsrfSaltHeader = "x-taurus-csrf-salt";
    private const int ProofBytes = 32;
    private const int EncodedProofCharacters = 44;

    public const string CsrfHeader = "x-taurus-csrf";
    public const string CookiePath = "/";
    public const string OriginHeader = "Origin";
    public const string UserIdHeader = "X-Taurus-User-Id";
    public const string UsernameHeader = "X-Taurus-User";
    public const string TenantHeader = "X-Taurus-Tenant";
    public const string NoStore = "no-store";
    public const string NoSniff = "nosniff";
    public const string FramePolicy = "frame-ancestors 'none'; base-uri 'none'";
    public const string WebRootDirectory = "wwwroot";
    public const string IndexFile = "index.html";
    public const string RequestPrefix = "/_taurus";
    public const int MaximumTokenLength = SessionTokens.MaximumTokenCharacters;

    public static string Origin(HttpContext context) =>
        string.Concat(context.Request.Scheme, Uri.SchemeDelimiter,
            context.Request.Host.Value);

    public static string SessionCookieName(HttpContext context) =>
        context.Request.IsHttps ? SessionCookie : DevelopmentSessionCookie;

    public static string CsrfCookieName(HttpContext context) =>
        context.Request.IsHttps ? CsrfCookie : DevelopmentCsrfCookie;

    public static CookieOptions Cookie(
        HttpContext context,
        DateTimeOffset? expires = null) => new()
    {
        Secure = context.Request.IsHttps,
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        Expires = expires
    };

    public static void RequireOrigin(HttpContext context)
    {
        var origin = context.Request.Headers[OriginHeader].ToString();

        if (!string.Equals(origin, Origin(context), StringComparison.Ordinal))
            Deny();
    }

    public static void RequireCsrf(HttpContext context, string seed)
    {
        RequireOrigin(context);

        var proof = context.Request.Headers[CsrfHeader].ToString();
        var salt = context.Request.Headers[CsrfSaltHeader].ToString();
        Span<byte> data = stackalloc byte[ProofBytes + ProofBytes];
        Span<byte> expected = stackalloc byte[ProofBytes];
        Span<byte> supplied = stackalloc byte[ProofBytes];

        if (proof.Length != EncodedProofCharacters
            || salt.Length != EncodedProofCharacters
            || !Convert.TryFromBase64String(seed, data[..ProofBytes], out var seedSize)
            || seedSize != ProofBytes
            || !Convert.TryFromBase64String(salt, data[ProofBytes..], out var saltSize)
            || saltSize != ProofBytes
            || !Convert.TryFromBase64String(proof, supplied, out var proofSize)
            || proofSize != ProofBytes)
            Deny();

        SHA256.HashData(data, expected);

        if (!CryptographicOperations.FixedTimeEquals(expected, supplied))
            Deny();
    }

    private static void Deny() => throw new RpcException(
        new Status(StatusCode.PermissionDenied, "Request could not be accepted"));

    public static string ReadSession(HttpContext context)
    {
        var token = context.Request.Cookies[SessionCookieName(context)];

        return token is not null && token.Length <= MaximumTokenLength
            ? token : string.Empty;
    }
}
