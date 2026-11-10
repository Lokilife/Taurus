using Microsoft.EntityFrameworkCore;
using Taurus.Core.Auditing;
using Taurus.Core.Authentication;
using Taurus.Core.Authentication.Policies;
using Taurus.Core.Credentials;
using Taurus.Core.Sessions;
using Taurus.Core.Users;
using Taurus.Infrastructure.Persistence;

namespace Taurus.Infrastructure.Persistence.Authentication;

public sealed class AuthRepo(
    IdentityDatabase db,
    IUserLogRepo userLogRepo) : IAuthRepo
{
    private const int IdleWriteMinutes = 5;
    private const int IdleWriteFraction = 2;

    private static readonly TimeSpan IdleWriteInterval =
        TimeSpan.FromMinutes(IdleWriteMinutes);

    public async Task<(UserEntity? User, CredentialEntity? Credential)> FindUserAsync(
        string tenantId,
        string username,
        CancellationToken cancellationToken)
    {
        var match = await (
            from user in db.Users.AsNoTracking()
            join credential in db.Credentials.AsNoTracking().Where(
                x => x.Type == CredentialType.Password
                    && x.Status == CredentialStatus.Active)
                on new { user.TenantId, UserId = user.Id }
                equals new { credential.TenantId, credential.UserId }
                into credentials
            from credential in credentials.DefaultIfEmpty()
            where user.TenantId == tenantId && user.Username == username
            select new { User = user, Credential = credential })
            .FirstOrDefaultAsync(cancellationToken);

        return (match?.User, match?.Credential);
    }

    public async Task<(AdminAuthenticationPolicyEntity Admin,
        UserAuthenticationPolicyEntity User)> GetPoliciesAsync(
            string tenantId,
            Guid userId,
            CancellationToken cancellationToken)
    {
        var admin = await db.AdminPolicies.AsNoTracking().SingleAsync(
            x => x.TenantId == tenantId, cancellationToken);
        var user = await db.UserPolicies.AsNoTracking().SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.UserId == userId, cancellationToken)
            ?? new UserAuthenticationPolicyEntity
            {
                TenantId = tenantId,
                UserId = userId
            };

        return (admin, user);
    }

    public async Task<bool> CreateSessionAsync(
        SessionEntity session,
        UserLogEntity audit,
        AuditFailureMode failureMode,
        CancellationToken cancellationToken)
    {
        db.Sessions.Add(session);
        userLogRepo.Add(audit);

        return await userLogRepo.SaveAsync(failureMode, cancellationToken);
    }

    public async Task<SessionIdentity?> GetSessionAsync(
        string tenantId,
        string origin,
        string digest,
        DateTimeOffset now,
        TimeSpan idleLifetime,
        CancellationToken cancellationToken)
    {
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.TokenDigest == digest
                && x.Origin == origin && !x.Revoked
                && x.IdleExpiresAt > now && x.AbsoluteExpiresAt > now,
            cancellationToken);

        if (session is null)
            return null;

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == session.UserId
                && x.Status == UserStatus.Enabled,
            cancellationToken);

        if (user is null)
            return null;

        var proposedExpiry = now + idleLifetime;
        var renewalInterval = TimeSpan.FromTicks(Math.Min(
            IdleWriteInterval.Ticks, idleLifetime.Ticks / IdleWriteFraction));

        if (proposedExpiry > session.AbsoluteExpiresAt)
            proposedExpiry = session.AbsoluteExpiresAt;

        if (proposedExpiry - session.IdleExpiresAt >= renewalInterval)
        {
            await db.Sessions.Where(x => x.TenantId == tenantId
                && x.TokenDigest == digest && !x.Revoked
                && x.IdleExpiresAt > now && x.AbsoluteExpiresAt > now
                && x.IdleExpiresAt < proposedExpiry)
                .ExecuteUpdateAsync(update => update.SetProperty(
                    x => x.IdleExpiresAt, proposedExpiry), cancellationToken);
        }

        return new SessionIdentity(user.Id, session.Id, user.Username,
            user.Locale, user.Timezone);
    }

    public async Task<bool> RevokeSessionAsync(
        string tenantId,
        string origin,
        string digest,
        AuditFailureMode failureMode,
        CancellationToken cancellationToken)
    {
        var session = await db.Sessions.SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.TokenDigest == digest
                && x.Origin == origin, cancellationToken);

        if (session is null || session.Revoked)
            return true;

        session.Revoked = true;

        userLogRepo.Add(new UserLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorId = session.UserId,
            Action = AuditAction.Logout,
            Severity = UserLogSeverity.Low,
            Succeeded = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        return await userLogRepo.SaveAsync(failureMode, cancellationToken);
    }
}
