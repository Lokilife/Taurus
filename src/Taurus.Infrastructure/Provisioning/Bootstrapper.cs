using Microsoft.EntityFrameworkCore;
using Taurus.Application.Features.Authentication.Login;
using Taurus.Core.Auditing;
using Taurus.Core.Authentication.Policies;
using Taurus.Core.Credentials;
using Taurus.Core.Credentials.Opaque;
using Taurus.Infrastructure.Authentication.Opaque;
using Taurus.Core.Provisioning;
using Taurus.Core.Users;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Persistence;
using Taurus.Infrastructure.Persistence.Schemas;

namespace Taurus.Infrastructure.Provisioning;

public sealed class Bootstrapper(
    IdentityDatabase db,
    Settings settings,
    IOpaqueService opaque,
    IUserLogRepo userLogRepo) : IBootstrapService
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        foreach (var tenant in settings.Tenants)
        {
            var password = tenant.RootPassword.Resolve();

            if (password.Length > LoginValidator.MaximumPasswordLength)
                throw new InvalidOperationException("Bootstrap password is too long");

            var existing = await db.Users.AnyAsync(
                x => x.TenantId == tenant.Id && x.Username == tenant.RootUsername,
                cancellationToken);

            if (existing)
                continue;

            var user = new UserEntity
            {
                TenantId = tenant.Id,
                Id = Guid.NewGuid(),
                Username = tenant.RootUsername,
                Status = UserStatus.Enabled,
                Version = Guid.NewGuid()
            };

            db.Users.Add(user);
            db.Credentials.Add(new CredentialEntity
            {
                TenantId = tenant.Id,
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Status = CredentialStatus.Active,
                Material = opaque.Enroll(
                    OpaqueService.Identifier(tenant.Id, tenant.RootUsername), password)
            });
            db.UserPolicies.Add(new UserAuthenticationPolicyEntity
            {
                TenantId = tenant.Id,
                UserId = user.Id
            });
            userLogRepo.Add(new UserLogEntity
            {
                TenantId = tenant.Id,
                Id = Guid.NewGuid(),
                ActorId = user.Id,
                Action = AuditAction.Bootstrap,
                Severity = UserLogSeverity.High,
                Succeeded = true,
                Timestamp = DateTimeOffset.UtcNow
            });

            if (!await db.Schemas.AnyAsync(x => x.TenantId == tenant.Id,
                cancellationToken))
            {
                db.Schemas.Add(new SchemaSnapshot
                {
                    TenantId = tenant.Id,
                    Definition = Settings.Schema(tenant),
                    Version = Guid.NewGuid()
                });
            }
        }

        foreach (var tenant in settings.Tenants)
        {
            var policy = await db.AdminPolicies.FindAsync(
                [tenant.Id], cancellationToken);

            var changed = policy is null
                || !SameFactors(policy.RequiredFactors, tenant.RequiredFactors)
                || !SameFactors(policy.AllowedFactors, tenant.AllowedFactors);

            if (policy is null)
            {
                policy = new AdminAuthenticationPolicyEntity { TenantId = tenant.Id };
                db.AdminPolicies.Add(policy);
            }

            policy.RequiredFactors = tenant.RequiredFactors;
            policy.AllowedFactors = tenant.AllowedFactors;

            if (changed)
            {
                userLogRepo.Add(new UserLogEntity
                {
                    TenantId = tenant.Id,
                    Id = Guid.NewGuid(),
                    Action = AuditAction.PolicyApply,
                    Severity = UserLogSeverity.High,
                    Succeeded = true,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }
        }

        if (!await userLogRepo.SaveAsync(AuditFailureMode.Block, cancellationToken))
            throw new InvalidOperationException("Required user audit could not be saved");

        await transaction.CommitAsync(cancellationToken);
    }

    private static bool SameFactors(CredentialType[] left, CredentialType[] right)
    {
        if (left.Length != right.Length)
            return false;

        for (var index = default(int); index < left.Length; index++)
        {
            if (left[index] != right[index])
                return false;
        }

        return true;
    }
}
