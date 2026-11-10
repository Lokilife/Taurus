using Taurus.Core.Credentials;

namespace Taurus.Core.Authentication.Policies;

public sealed class AdminAuthenticationPolicyEntity
{
    public string TenantId { get; set; } = string.Empty;
    public CredentialType[] RequiredFactors { get; set; } = [CredentialType.Password];
    public CredentialType[] AllowedFactors { get; set; } = [CredentialType.Password];
}
