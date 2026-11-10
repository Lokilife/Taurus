using Taurus.Core.Credentials;

namespace Taurus.Core.Authentication.Policies;

public sealed class UserAuthenticationPolicyEntity
{
    public string TenantId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public CredentialType[] RequiredFactors { get; set; } = [];
}
