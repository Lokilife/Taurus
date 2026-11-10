using System.Security.Cryptography;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Taurus.Infrastructure.Authentication.Keys;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Server.Security;

namespace Taurus.Tests.tests;

public class BrowserSecurityTests
{
    private const string Tenant = "tenant";
    private const string OtherTenant = "other";
    private const string Origin = "https://app.localhost";
    private const string Host = "app.localhost";
    private const int KeyBytes = 32;
    private const int DoubleKeyBytes = KeyBytes + KeyBytes;

    [Fact]
    public void ProofRequiresServerSeedSaltAndExactOrigin()
    {
        using var keys = new AuthenticationKeys(new AuthenticationKeyOptions
        {
            Csrf = new SecretReference
            {
                Value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(KeyBytes))
            }
        });
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid().ToString();
        var seed = keys.CsrfSeed(Tenant, Origin, userId, sessionId);
        var context = new DefaultHttpContext();
        var data = new byte[DoubleKeyBytes];
        var salt = RandomNumberGenerator.GetBytes(KeyBytes);

        context.Request.Scheme = Uri.UriSchemeHttps;
        context.Request.Host = new HostString(Host);
        context.Request.Headers[BrowserSecurity.OriginHeader] = Origin;
        Convert.FromBase64String(seed).CopyTo(data, default(int));
        salt.CopyTo(data, KeyBytes);
        context.Request.Headers[BrowserSecurity.CsrfSaltHeader] =
            Convert.ToBase64String(salt);
        context.Request.Headers[BrowserSecurity.CsrfHeader] =
            Convert.ToBase64String(SHA256.HashData(data));

        BrowserSecurity.RequireCsrf(context, seed);

        Assert.Throws<RpcException>(() => BrowserSecurity.RequireCsrf(context,
            keys.CsrfSeed(OtherTenant, Origin, userId, sessionId)));

        context.Request.Headers.Remove(BrowserSecurity.OriginHeader);

        Assert.Throws<RpcException>(() => BrowserSecurity.RequireCsrf(context, seed));
    }
}
