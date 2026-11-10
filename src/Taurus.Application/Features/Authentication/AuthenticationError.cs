using FluentResults;
using Taurus.Core.Authentication;

namespace Taurus.Application.Features.Authentication;

public sealed class AuthenticationError(AuthenticationFailure failure) : Error
{
    public AuthenticationFailure Failure { get; } = failure;
}
