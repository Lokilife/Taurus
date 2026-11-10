using Taurus.Core.Credentials;
using Taurus.Infrastructure.Configuration.Attributes;
using Taurus.Infrastructure.Configuration.Secrets;

namespace Taurus.Infrastructure.Configuration.Tenants;

public sealed class Tenant
{
    public string Id { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public List<string> ApplicationOrigins { get; set; } = [];
    public string RootUsername { get; set; } = "root";
    public SecretReference RootPassword { get; set; } = new();
    public CredentialType[] RequiredFactors { get; set; } = [CredentialType.Password];
    public CredentialType[] AllowedFactors { get; set; } = [CredentialType.Password];
    public List<AttributeDefinition> Attributes { get; set; } = [];
}
