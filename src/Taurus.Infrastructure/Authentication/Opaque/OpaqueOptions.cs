using Taurus.Infrastructure.Configuration.Secrets;

namespace Taurus.Infrastructure.Authentication.Opaque;

public sealed class OpaqueOptions
{
    [SecretOption]
    public SecretReference Setup { get; set; } = new();
    public int MaximumPendingLogins { get; set; } = 1024;
    public TimeSpan ChallengeLifetime { get; set; } = TimeSpan.FromMinutes(2);
}
