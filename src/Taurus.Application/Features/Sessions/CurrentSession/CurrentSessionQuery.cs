using MediatR;
using Taurus.Core.Sessions;

namespace Taurus.Application.Features.Sessions.CurrentSession;

public sealed record CurrentSessionQuery(
    string TenantId,
    string Origin,
    string Token) : IRequest<SessionIdentity?>
{
    public override string ToString() => nameof(CurrentSessionQuery);
}
