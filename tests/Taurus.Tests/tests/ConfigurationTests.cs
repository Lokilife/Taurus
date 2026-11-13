using SerializerBuilder = YamlDotNet.Serialization.SerializerBuilder;
using YamlDotNet.Serialization.NamingConventions;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Infrastructure.Configuration.Tenants;

namespace Taurus.Tests.tests;

public class ConfigurationTests
{
    private const string Secret = "sensitive-test-value";
    private const string RootPasswordPath = "tenants[0].rootPassword";
    private const string DatabasePath = "database";
    private const string ExclusiveSources = "exactly one";

    private const string Origin = "https://app.localhost";
    private const string TenantId = "test";
    private const string SecretFileContent = " password \n";
    private const string SecretFileValue = " password ";

    [Fact]
    public void SchemaContextDistinguishesAliasedSecretLocations()
    {
        var shared = new SecretReference
        {
            File = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), Secret)
        };
        var settings = new Settings
        {
            NoRateLimits = true,
            Database = shared,
            Tenants = [new Tenant
            {
                Id = TenantId,
                Issuer = Origin,
                ApplicationOrigins = [Origin],
                RootPassword = shared
            }]
        };

        settings.Keys.Csrf.Value = Secret;
        settings.Opaque.Setup.Value = Secret;
        settings.Validate();

        var database = Assert.Throws<InvalidOperationException>(() =>
            settings.Database.Resolve());
        var password = Assert.Throws<InvalidOperationException>(() =>
            settings.Tenants[default(int)].RootPassword.Resolve());

        Assert.Contains(DatabasePath, database.Message);
        Assert.Contains(RootPasswordPath, password.Message);
        Assert.DoesNotContain(Secret, password.ToString());
    }

    [Fact]
    public void TenantPasswordErrorIdentifiesTheOptionWithoutSecretValues()
    {
        var settings = new Settings
        {
            NoRateLimits = true,
            Database = new SecretReference { Value = Secret },
            Tenants = [new Tenant
            {
                Id = TenantId,
                Issuer = Origin,
                ApplicationOrigins = [Origin],
                RootPassword = new SecretReference
                {
                    Value = Secret,
                    Environment = Secret
                }
            }]
        };

        settings.Keys.Csrf.Value = Secret;
        settings.Opaque.Setup.Value = Secret;

        var path = Path.GetTempFileName();

        try
        {
            var yaml = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build().Serialize(settings);

            File.WriteAllText(path, yaml);

            var error = Assert.Throws<InvalidOperationException>(() =>
                Settings.Load(path));

            Assert.Contains(RootPasswordPath,
                error.Message);
            Assert.Contains(ExclusiveSources, error.Message);
            Assert.DoesNotContain(Secret, error.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

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
