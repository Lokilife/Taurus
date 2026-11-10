using FluentResults;
using MediatR;

namespace Taurus.Application.Features.Authentication.Login;

public sealed record LoginCommand(
    string TenantId,
    string Origin,
    string Username,
    string ChallengeId,
    string Request) : IRequest<Result<string>>
{
    public override string ToString() => nameof(LoginCommand);
}
