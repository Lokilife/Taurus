using FluentValidation;
using Taurus.Application.Features.Authentication.Login;

namespace Taurus.Application.Features.Authentication.BeginLogin;

public sealed class BeginLoginValidator : AbstractValidator<BeginLoginCommand>
{
    public BeginLoginValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Origin).NotEmpty();
        RuleFor(x => x.Username).NotEmpty()
            .MaximumLength(LoginValidator.MaximumUsernameLength);
        RuleFor(x => x.Request).NotEmpty()
            .MaximumLength(LoginValidator.MaximumOpaqueMessageLength);
    }
}
