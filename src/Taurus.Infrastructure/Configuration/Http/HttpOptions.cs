using System.Net;
using Taurus.Infrastructure.Configuration.Secrets;

namespace Taurus.Infrastructure.Configuration.Http;

public sealed class HttpOptions
{
    private const string RootPath = "/";
    private const string EmptySegment = "//";
    private const string BracketedLoopback = "[::1]";

    public string RequestPrefix { get; set; } = "/_taurus";
    public bool AllowLoopbackHttp { get; set; }
    public string? CertificateFile { get; set; }
    [SecretOption(required: false)]
    public SecretReference? CertificatePassword { get; set; }

    public void Validate()
    {
        if (RequestPrefix.Length != default
            && (!RequestPrefix.StartsWith(RootPath, StringComparison.Ordinal)
                || RequestPrefix.EndsWith(RootPath, StringComparison.Ordinal)))
            throw new InvalidOperationException("Prefix needs a leading slash only");

        foreach (var character in RequestPrefix)
        {
            if (!char.IsAsciiLetterOrDigit(character)
                && character is not '/' and not '-' and not '_')
                throw new InvalidOperationException("Invalid request prefix");
        }

        if (RequestPrefix.Contains(EmptySegment, StringComparison.Ordinal))
            throw new InvalidOperationException("Empty prefix segment");

        if (CertificatePassword is not null)
        {
            if (string.IsNullOrWhiteSpace(CertificateFile))
                throw new InvalidOperationException("Certificate password needs a file");

            CertificatePassword.Validate();
        }
    }

    public bool AllowsDevelopmentOrigin(Uri uri) =>
        AllowLoopbackHttp && uri.Scheme == Uri.UriSchemeHttp
        && (string.Equals(uri.Host, IPAddress.Loopback.ToString(),
                StringComparison.Ordinal)
            || string.Equals(uri.Host, IPAddress.IPv6Loopback.ToString(),
                StringComparison.Ordinal)
            || string.Equals(uri.Host, BracketedLoopback, StringComparison.Ordinal));
}
