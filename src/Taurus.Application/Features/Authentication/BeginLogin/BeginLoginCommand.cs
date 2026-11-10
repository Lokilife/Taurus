using MediatR;
using Taurus.Core.Credentials.Opaque;

namespace Taurus.Application.Features.Authentication.BeginLogin;

public sealed record BeginLoginCommand(
    string TenantId,
    string Origin,
    string Username,
    string Request) : IRequest<OpaqueChallenge>
{
    public override string ToString() => nameof(BeginLoginCommand);
}
