using Taurus.Core.Auditing;

namespace Taurus.Application.Features.Authentication;

public sealed class AuthenticationOptions
{
    public TimeSpan IdleLifetime { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan AbsoluteLifetime { get; set; } = TimeSpan.FromDays(30);
    public AuditFailureMode AuditFailureMode { get; set; } = AuditFailureMode.Block;
}
