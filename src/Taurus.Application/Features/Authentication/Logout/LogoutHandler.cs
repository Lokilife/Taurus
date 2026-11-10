using MediatR;
using Taurus.Application.Features.Authentication;
using Taurus.Application.Features.Sessions;
using Taurus.Core.Auditing;
using Taurus.Core.Authentication;

namespace Taurus.Application.Features.Authentication.Logout;

public sealed class LogoutHandler(
    IAuthRepo repo,
    AuthenticationOptions options) : IRequestHandler<LogoutCommand, bool>
{
    public Task<bool> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken) => repo.RevokeSessionAsync(
            request.TenantId,
            request.Origin,
            SessionTokens.Digest(request.Token),
            options.AuditFailureMode,
            cancellationToken);
}
