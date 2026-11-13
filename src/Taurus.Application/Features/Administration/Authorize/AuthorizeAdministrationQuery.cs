using FluentResults;
using MediatR;
using Taurus.Core.Sessions;

namespace Taurus.Application.Features.Administration.Authorize;

/// <summary>Authorizes a session after the transport verifies the admin origin.</summary>
public sealed record AuthorizeAdministrationQuery(
    string TenantId,
    string Origin,
    string Token) : IRequest<Result<SessionIdentity>>
{
    public override string ToString() => nameof(AuthorizeAdministrationQuery);
}
