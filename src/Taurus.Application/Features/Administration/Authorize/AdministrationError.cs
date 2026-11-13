using FluentResults;

namespace Taurus.Application.Features.Administration.Authorize;

public sealed class AdministrationError(AdministrationFailure failure) : Error
{
    public AdministrationFailure Failure { get; } = failure;
}
