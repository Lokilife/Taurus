using Taurus.Infrastructure.Authentication.Keys;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Infrastructure.Configuration.Tenants;

namespace Taurus.Tests;

public static class TestSettings
{
    public const string ConnectionString = "Data Source=:memory:";

    public static Settings Create(Tenant tenant)
    {
        var settings = new Settings
        {
            NoRateLimits = true,
            Database = new SecretReference { Value = ConnectionString },
            Keys = new()
            {
                Csrf = new SecretReference { Value = KeyBootstrap.GenerateCsrfKey() }
            },
            Opaque = new()
            {
                Setup = new SecretReference { Value = KeyBootstrap.GenerateOpaqueSetup() }
            },
            Tenants = [tenant]
        };

        settings.Validate();

        return settings;
    }
}
