using FluentResults;
using MediatR;
using Taurus.Application.Features.Auditing.RecordUserLog;
using Taurus.Application.Features.Authentication;
using Taurus.Application.Features.Authentication.Policies;
using Taurus.Application.Features.Sessions;
using Taurus.Core.Auditing;
using Taurus.Core.Authentication;
using Taurus.Core.Credentials;
using Taurus.Core.Credentials.Opaque;
using Taurus.Core.Sessions;
using Taurus.Core.Users;

namespace Taurus.Application.Features.Authentication.Login;

public sealed class LoginHandler(
    IAuthRepo repo,
    ISender sender,
    IOpaqueService opaque,
    AuthenticationOptions options,
    TimeProvider clock) : IRequestHandler<LoginCommand, Result<string>>
{
    private const int OpaqueFormatVersion = 1;

    public async Task<Result<string>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var (user, credential) = await repo.FindUserAsync(
            request.TenantId, request.Username, cancellationToken);
        var valid = opaque.Finish(request.TenantId, request.Origin, request.Username,
            credential?.Material, request.ChallengeId, request.Request);
        var accepted = valid && user?.Status == UserStatus.Enabled
            && credential?.Status == CredentialStatus.Active
            && credential.FormatVersion == OpaqueFormatVersion;
        var now = clock.GetUtcNow();
        var audit = new UserLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ActorId = user?.Id,
            Action = AuditAction.Login,
            Severity = UserLogSeverity.Middle,
            Succeeded = accepted,
            Timestamp = now
        };

        if (accepted)
        {
            var policies = await repo.GetPoliciesAsync(
                request.TenantId, user!.Id, cancellationToken);

            try
            {
                AuthenticationPolicy.Evaluate(policies.Admin, policies.User);
            }
            catch (InvalidOperationException)
            {
                accepted = false;
                audit.Succeeded = false;
            }
        }

        if (!accepted)
        {
            var recorded = await sender.Send(new RecordUserLogCommand(
                audit, options.AuditFailureMode), cancellationToken);

            return Result.Fail<string>(new AuthenticationError(recorded
                ? AuthenticationFailure.InvalidCredentials
                : AuthenticationFailure.Unavailable));
        }

        var token = SessionTokens.Create();
        var session = new SessionEntity
        {
            TenantId = request.TenantId,
            Id = Guid.NewGuid(),
            UserId = user!.Id,
            Origin = request.Origin,
            TokenDigest = SessionTokens.Digest(token),
            CreatedAt = now,
            IdleExpiresAt = now + options.IdleLifetime,
            AbsoluteExpiresAt = now + options.AbsoluteLifetime
        };
        var created = await repo.CreateSessionAsync(
            session, audit, options.AuditFailureMode, cancellationToken);

        return created
            ? Result.Ok(token)
            : Result.Fail<string>(new AuthenticationError(
                AuthenticationFailure.Unavailable));
    }
}
