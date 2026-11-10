using FluentValidation;

namespace Taurus.Application.Features.Authentication.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public const int MaximumUsernameLength = 128;
    public const int MaximumPasswordLength = 1024;
    public const int MaximumOpaqueMessageLength = 4096;

    public LoginValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Origin).NotEmpty();
        RuleFor(x => x.Username).NotEmpty().MaximumLength(MaximumUsernameLength);
        RuleFor(x => x.ChallengeId).NotEmpty()
            .MaximumLength(MaximumOpaqueMessageLength);
        RuleFor(x => x.Request).NotEmpty().MaximumLength(MaximumOpaqueMessageLength);
    }
}
