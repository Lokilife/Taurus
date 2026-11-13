using Taurus.Infrastructure.Configuration.Secrets;

namespace Taurus.Infrastructure.Authentication.Keys;

public sealed class AuthenticationKeyOptions
{
    [SecretOption]
    public SecretReference Csrf { get; set; } = new();
    public string Version { get; set; } = "1";
}
