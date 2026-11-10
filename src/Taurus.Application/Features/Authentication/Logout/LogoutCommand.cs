using MediatR;

namespace Taurus.Application.Features.Authentication.Logout;

public sealed record LogoutCommand(
    string TenantId,
    string Origin,
    string Token) : IRequest<bool>
{
    public override string ToString() => nameof(LogoutCommand);
}
