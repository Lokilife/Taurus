using MediatR;
using Taurus.Application.Features.Authentication;
using Taurus.Application.Features.Sessions;
using Taurus.Core.Authentication;
using Taurus.Core.Sessions;

namespace Taurus.Application.Features.Sessions.CurrentSession;

public sealed class CurrentSessionHandler(
    IAuthRepo repo,
    AuthenticationOptions options,
    TimeProvider clock) : IRequestHandler<CurrentSessionQuery, SessionIdentity?>
{
    public Task<SessionIdentity?> Handle(
        CurrentSessionQuery request,
        CancellationToken cancellationToken) => repo.GetSessionAsync(
            request.TenantId,
            request.Origin,
            SessionTokens.Digest(request.Token),
            clock.GetUtcNow(),
            options.IdleLifetime,
            cancellationToken);
}
