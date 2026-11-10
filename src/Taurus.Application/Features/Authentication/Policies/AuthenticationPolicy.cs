using Taurus.Core.Authentication.Policies;
using Taurus.Core.Credentials;

namespace Taurus.Application.Features.Authentication.Policies;

public static class AuthenticationPolicy
{
    public static IReadOnlySet<CredentialType> Evaluate(
        AdminAuthenticationPolicyEntity admin,
        UserAuthenticationPolicyEntity user)
    {
        if (admin.TenantId != user.TenantId)
            throw new InvalidOperationException("Tenant mismatch");

        var required = new HashSet<CredentialType>(admin.RequiredFactors);

        foreach (var factor in user.RequiredFactors)
            required.Add(factor);

        foreach (var factor in required)
        {
            var allowed = false;

            foreach (var candidate in admin.AllowedFactors)
            {
                if (candidate == factor)
                    allowed = true;
            }

            if (!allowed)
                throw new InvalidOperationException("Policy cannot be satisfied");

            if (factor != CredentialType.Password)
                throw new InvalidOperationException("Unsupported factor");
        }

        if (required.Count == default)
            throw new InvalidOperationException("Authentication requires a factor");

        return required;
    }
}
