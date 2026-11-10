using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Infrastructure.Configuration.Tenants;

namespace Taurus.Tests.tests;

public class ConfigurationTests
{
    private const string Origin = "https://app.localhost";
    private const string TenantId = "test";
    private const string SecretFileContent = " password \n";
    private const string SecretFileValue = " password ";

    [Fact]
    public void InlineSecretsAreExplicitAndExclusive()
    {
        var reference = new SecretReference { Value = SecretFileValue };

        Assert.Equal(SecretFileValue, reference.Resolve());

        reference.Environment = nameof(Settings.Database);

        Assert.Throws<InvalidOperationException>(reference.Validate);
    }

    [Fact]
    public void DuplicateOriginsAndImplicitNoRateLimitsAreRejected()
    {
        var settings = new Settings
        {
            Database = new SecretReference { Environment = nameof(Settings.Database) },
            Tenants = [new Tenant
            {
                Id = TenantId,
                Issuer = Origin,
                RootPassword = new SecretReference
                {
                    Environment = nameof(Tenant.RootPassword)
                },
                ApplicationOrigins = [Origin, Origin]
            }]
        };

        settings.Opaque.Setup.Environment = nameof(Settings.Opaque);
        settings.Keys.Csrf.Environment = nameof(Settings.Keys);

        Assert.Throws<InvalidOperationException>(settings.Validate);

        settings.NoRateLimits = true;

        Assert.Throws<InvalidOperationException>(settings.Validate);

        settings.Tenants[default(int)].ApplicationOrigins.RemoveAt(default(int));
        settings.Validate();
    }

    [Fact]
    public void UnknownYamlFieldsAreRejected()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "undocumented: true");

            Assert.Throws<YamlDotNet.Core.YamlException>(() => Settings.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FileSecretsPreserveSpacesAndIgnoreAnEmptyEnvironmentReference()
    {
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, SecretFileContent);

            var reference = new SecretReference
            {
                Environment = string.Empty,
                File = path
            };

            Assert.Equal(SecretFileValue, reference.Resolve());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
