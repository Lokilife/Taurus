using MediatR;
using Taurus.Core.Authentication;
using Taurus.Core.Auditing;
using Taurus.Application.Features.Authentication;
using Taurus.Application.Features.Auditing.RecordUserLog;
using Taurus.Core.Credentials.Opaque;

namespace Taurus.Application.Features.Authentication.BeginLogin;

public sealed class BeginLoginHandler(
    IAuthRepo repo,
    IOpaqueService opaque,
    ISender sender,
    AuthenticationOptions options,
    TimeProvider clock)
    : IRequestHandler<BeginLoginCommand, OpaqueChallenge>
{
    public async Task<OpaqueChallenge> Handle(
        BeginLoginCommand request,
        CancellationToken cancellationToken)
    {
        var (user, credential) = await repo.FindUserAsync(
            request.TenantId, request.Username, cancellationToken);

        var recorded = await sender.Send(new RecordUserLogCommand(new UserLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ActorId = user?.Id,
            Action = AuditAction.LoginStarted,
            Severity = UserLogSeverity.Middle,
            Succeeded = true,
            Timestamp = clock.GetUtcNow()
        }, options.AuditFailureMode), cancellationToken);

        if (!recorded)
            throw new InvalidOperationException("Authentication unavailable");

        return opaque.Begin(request.TenantId, request.Origin, request.Username,
            credential?.Material, request.Request);
    }
}
