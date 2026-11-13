using MediatR;
using Taurus.Application.Features.Sessions.CurrentSession;
using Taurus.Core.Administration;
using Taurus.Core.Users.Profiles;

namespace Taurus.Application.Features.Users.CurrentUser;

public sealed class CurrentUserHandler(
    ISender sender,
    IUserProfileRepo repo,
    IAdministrationOriginPolicy policy)
    : IRequestHandler<CurrentUserQuery, CurrentUserProfile?>
{
    public async Task<CurrentUserProfile?> Handle(
        CurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        var identity = await sender.Send(new CurrentSessionQuery(
            request.TenantId, request.Origin, request.Token), cancellationToken);

        if (identity is null)
            return null;

        var profile = await repo.FindAsync(request.TenantId, identity.UserId,
            cancellationToken);

        if (profile is null)
            return null;

        return new CurrentUserProfile(profile, profile.Root
            && policy.Allows(request.TenantId, request.Origin));
    }
}
