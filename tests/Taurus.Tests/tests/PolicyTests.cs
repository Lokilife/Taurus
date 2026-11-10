using Taurus.Application.Features.Authentication.Policies;
using Taurus.Core.Credentials;

namespace Taurus.Tests.tests;

public class PolicyTests
{
    private const string TenantA = "a";
    private const string TenantB = "b";

    [Fact]
    public void AdminRequirementsCannotBeRemovedByUser()
    {
        var result = AuthenticationPolicy.Evaluate(
            new() { TenantId = TenantA },
            new() { TenantId = TenantA });

        Assert.Contains(CredentialType.Password, result);
    }

    [Fact]
    public void CrossTenantPolicyIsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => AuthenticationPolicy.Evaluate(
            new() { TenantId = TenantA },
            new() { TenantId = TenantB }));
    }

    [Fact]
    public void DisallowedUserFactorIsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => AuthenticationPolicy.Evaluate(
            new() { TenantId = TenantA },
            new() { TenantId = TenantA, RequiredFactors = [CredentialType.Totp] }));
    }
}
