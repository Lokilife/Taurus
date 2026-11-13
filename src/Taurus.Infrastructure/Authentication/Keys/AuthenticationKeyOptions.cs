using Taurus.Infrastructure.Configuration.Secrets;

namespace Taurus.Infrastructure.Authentication.Keys;

public sealed class AuthenticationKeyOptions
{
    public SecretReference Csrf { get; set; } = new()
    {
        ConfigurationPath = "keys.csrf"
    };
    public string Version { get; set; } = "1";
}
