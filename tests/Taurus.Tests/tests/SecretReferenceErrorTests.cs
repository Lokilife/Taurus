using SerializerBuilder = YamlDotNet.Serialization.SerializerBuilder;
using YamlDotNet.Serialization.NamingConventions;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Infrastructure.Configuration.Tenants;

namespace Taurus.Tests.tests;

public class SecretReferenceErrorTests
{
    private const string Secret = "sensitive-test-value";
    private const string Origin = "https://app.localhost";
    private const string TenantId = "test";
    private const string MissingEnvironment = "TAURUS_TEST_MISSING_SECRET_SOURCE";
    private const int FirstTenant = 0;
    private const string ExclusiveSources = "exactly one";
    private const string MissingSource = "missing or empty";
    private const string UnreadableSource = "cannot read";

    [Fact]
    public void EmptyAndNullReferencesIdentifyTheOption()
    {
        var missing = Assert.Throws<InvalidOperationException>(() =>
            SecretReference.Validate(null, SecretReferencePaths.Database));
        var empty = Assert.Throws<InvalidOperationException>(() =>
            SecretReference.Validate(new SecretReference(), SecretReferencePaths.Csrf));

        Assert.Contains(SecretReferencePaths.Database, missing.Message);
        Assert.Contains(SecretReferencePaths.Csrf, empty.Message);
        Assert.Contains(ExclusiveSources, empty.Message);
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

            Assert.Contains(SecretReferencePaths.RootPassword(FirstTenant),
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
    public void MissingEnvironmentKeepsOptionContextDuringResolution()
    {
        var reference = new SecretReference { Environment = MissingEnvironment };

        Environment.SetEnvironmentVariable(MissingEnvironment, null);
        reference.Validate(SecretReferencePaths.OpaqueSetup);

        var error = Assert.Throws<InvalidOperationException>(() => reference.Resolve(
                SecretReferencePaths.OpaqueSetup));

        Assert.Contains(SecretReferencePaths.OpaqueSetup, error.Message);
        Assert.Contains(MissingSource, error.Message);
        Assert.DoesNotContain(MissingEnvironment, error.ToString());
    }

    [Fact]
    public void UnreadableFileDoesNotExposeItsConfiguredValue()
    {
        var reference = new SecretReference
        {
            File = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), Secret)
        };

        reference.Validate(SecretReferencePaths.CertificatePassword);

        var error = Assert.Throws<InvalidOperationException>(() => reference.Resolve(
                SecretReferencePaths.CertificatePassword));

        Assert.Contains(SecretReferencePaths.CertificatePassword, error.Message);
        Assert.Contains(UnreadableSource, error.Message);
        Assert.DoesNotContain(Secret, error.ToString());
        Assert.Null(error.InnerException);
    }
}
