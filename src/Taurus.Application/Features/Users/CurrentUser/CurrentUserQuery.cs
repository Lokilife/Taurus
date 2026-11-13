using MediatR;

namespace Taurus.Application.Features.Users.CurrentUser;

/// <summary>Loads a profile from trusted tenant/origin context and a token.</summary>
public sealed record CurrentUserQuery(string TenantId, string Origin, string Token)
    : IRequest<CurrentUserProfile?>
{
    public override string ToString() => nameof(CurrentUserQuery);
}
