using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Taurus.Application.Features.Authentication;
using Taurus.Application.Features.Authentication.Login;
using Taurus.Application.Features.Authentication.Policies;
using Taurus.Core.Auditing;
using Taurus.Core.Authentication.Policies;
using Taurus.Core.Credentials;
using Taurus.Infrastructure.Authentication.Opaque;
using Taurus.Infrastructure.Authentication.Keys;
using Taurus.Infrastructure.Configuration.Http;
using Taurus.Infrastructure.Configuration.Persistence;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Infrastructure.Configuration.Tenants;
using Taurus.Infrastructure.Logging;

namespace Taurus.Infrastructure.Configuration;

public sealed class Settings
{
    private const string RootPath = "/";
    private const int MaximumIdentifierLength = 128;
    private const int MinimumPositiveValue = 1;

    private readonly Dictionary<string, Tenant> originTenants =
        new(StringComparer.OrdinalIgnoreCase);

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;
    public SecretReference Database { get; set; } = new();
    public List<string> TenantFiles { get; set; } = [];
    public List<Tenant> Tenants { get; set; } = [];
    public bool NoRateLimits { get; set; }
    public AuthenticationOptions Sessions { get; set; } = new();
    public ServiceLogOptions ServiceLogs { get; set; } = new();
    public AuthenticationKeyOptions Keys { get; set; } = new();
    public OpaqueOptions Opaque { get; set; } = new();
    public HttpOptions Http { get; set; } = new();
    public string[] ListenUrls { get; set; } = [];
    public List<string> TrustedProxies { get; set; } = [];

    public static Settings Load(string path)
    {
        var yaml = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithDuplicateKeyChecking()
            .Build();
        var settings = yaml.Deserialize<Settings>(System.IO.File.ReadAllText(path))
            ?? throw new InvalidOperationException("Empty configuration");
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;

        foreach (var file in settings.TenantFiles)
        {
            var tenantPath = Path.Combine(directory, file);

            settings.Tenants.Add(yaml.Deserialize<Tenant>(
                System.IO.File.ReadAllText(tenantPath)));
        }

        settings.Validate();

        return settings;
    }

    public void Validate()
    {
        if (!Enum.IsDefined(Provider) || Tenants.Count == default || !NoRateLimits)
            throw new InvalidOperationException(
                "Invalid provider, missing tenants or explicit NoRateLimits required");

        if (Sessions.IdleLifetime <= TimeSpan.Zero
            || Sessions.AbsoluteLifetime < Sessions.IdleLifetime
            || !Enum.IsDefined(Sessions.AuditFailureMode))
            throw new InvalidOperationException("Invalid session settings");

        Keys.Csrf.Validate();
        Opaque.Setup.Validate();

        if (Opaque.MaximumPendingLogins <= default(int)
            || Opaque.ChallengeLifetime <= TimeSpan.Zero)
            throw new InvalidOperationException("Invalid OPAQUE challenge settings");

        if (string.IsNullOrWhiteSpace(ServiceLogs.Directory)
            || ServiceLogs.MaximumFileBytes <= default(long)
            || ServiceLogs.RetainedFiles <= default(int)
            || !Enum.IsDefined(ServiceLogs.MinimumLevel))
            throw new InvalidOperationException("Invalid service logging settings");

        Http.Validate();
        Database.Validate();
        originTenants.Clear();

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var issuers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tenant in Tenants)
        {
            if (string.IsNullOrWhiteSpace(tenant.Id)
                || tenant.Id.Length > MaximumIdentifierLength || !ids.Add(tenant.Id)
                || string.IsNullOrWhiteSpace(tenant.RootUsername)
                || tenant.RootUsername.Length > LoginValidator.MaximumUsernameLength)
                throw new InvalidOperationException("Invalid tenant identifiers");

            tenant.RootPassword.Validate();
            ValidateOrigin(tenant.Issuer);

            if (!issuers.Add(tenant.Issuer))
                throw new InvalidOperationException("Duplicate tenant issuer");

            if (tenant.ApplicationOrigins.Count == default)
                throw new InvalidOperationException("Tenant needs application origins");

            foreach (var origin in tenant.ApplicationOrigins)
            {
                ValidateOrigin(origin);

                if (!origins.Add(origin))
                    throw new InvalidOperationException(
                        "Conflicting application origins");

                originTenants.Add(origin, tenant);
            }

            AuthenticationPolicy.Evaluate(new AdminAuthenticationPolicyEntity
            {
                TenantId = tenant.Id,
                RequiredFactors = tenant.RequiredFactors,
                AllowedFactors = tenant.AllowedFactors
            }, new UserAuthenticationPolicyEntity { TenantId = tenant.Id });

            foreach (var factor in tenant.AllowedFactors)
            {
                if (factor != CredentialType.Password)
                    throw new InvalidOperationException("Unsupported allowed factor");
            }

            var attributes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var attribute in tenant.Attributes)
            {
                if (string.IsNullOrWhiteSpace(attribute.Name)
                    || attribute.Name.Length > MaximumIdentifierLength
                    || !attributes.Add(attribute.Name)
                    || !Enum.IsDefined(attribute.Kind)
                    || attribute.MaximumLength < MinimumPositiveValue)
                    throw new InvalidOperationException("Invalid attribute definition");
            }

            tenant.Attributes.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.Name, right.Name));
        }

        foreach (var proxy in TrustedProxies)
        {
            if (!System.Net.IPAddress.TryParse(proxy, out _))
                throw new InvalidOperationException("Invalid trusted proxy address");
        }
    }

    public Tenant? Resolve(string origin) =>
        originTenants.TryGetValue(origin, out var tenant) ? tenant : null;

    public static string Schema(Tenant tenant) =>
        JsonSerializer.Serialize(tenant.Attributes);

    private void ValidateOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && !Http.AllowsDevelopmentOrigin(uri))
            || uri.AbsolutePath != RootPath
            || uri.Query != string.Empty || uri.Fragment != string.Empty
            || uri.UserInfo != string.Empty
            || !string.Equals(origin, uri.GetLeftPart(UriPartial.Authority),
                StringComparison.Ordinal))
            throw new InvalidOperationException("Use a canonical HTTPS origin");
    }
}
