using Taurus.Core.Auditing;
using Taurus.Core.Authentication.Policies;
using Taurus.Core.Credentials;
using Taurus.Core.Sessions;
using Taurus.Core.Users;

namespace Taurus.Core.Authentication;

public interface IAuthRepo
{
    Task<(UserEntity? User, CredentialEntity? Credential)> FindUserAsync(
        string tenantId,
        string username,
        CancellationToken cancellationToken);

    Task<(AdminAuthenticationPolicyEntity Admin,
        UserAuthenticationPolicyEntity User)> GetPoliciesAsync(
            string tenantId,
            Guid userId,
            CancellationToken cancellationToken);

    Task<bool> CreateSessionAsync(
        SessionEntity session,
        UserLogEntity audit,
        AuditFailureMode failureMode,
        CancellationToken cancellationToken);

    Task<SessionIdentity?> GetSessionAsync(
        string tenantId,
        string origin,
        string digest,
        DateTimeOffset now,
        TimeSpan idleLifetime,
        CancellationToken cancellationToken);

    Task<bool> RevokeSessionAsync(
        string tenantId,
        string origin,
        string digest,
        AuditFailureMode failureMode,
        CancellationToken cancellationToken);
}
