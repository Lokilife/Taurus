using FluentResults;
using MediatR;
using Taurus.Application.Features.Sessions.CurrentSession;
using Taurus.Core.Administration;
using Taurus.Core.Sessions;

namespace Taurus.Application.Features.Administration.Authorize;

public sealed class AuthorizeAdministrationHandler(
    ISender sender,
    IAdministrationRepo repo)
    : IRequestHandler<AuthorizeAdministrationQuery, Result<SessionIdentity>>
{
    public async Task<Result<SessionIdentity>> Handle(
        AuthorizeAdministrationQuery request,
        CancellationToken cancellationToken)
    {
        var identity = await sender.Send(new CurrentSessionQuery(
            request.TenantId, request.Origin, request.Token), cancellationToken);

        if (identity is null)
            return Result.Fail<SessionIdentity>(new AdministrationError(
                AdministrationFailure.Unauthenticated));

        if (!await repo.IsRootAsync(request.TenantId, identity.UserId,
            cancellationToken))
            return Result.Fail<SessionIdentity>(new AdministrationError(
                AdministrationFailure.Forbidden));

        return Result.Ok(identity);
    }
}
