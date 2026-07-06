using System.Text.RegularExpressions;

using FluentValidation;

namespace Taurus.Application.Features.Users.CreateUser;

public partial class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().Matches(UsernameRegex()).MinimumLength(3).MaximumLength(256);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }

    [GeneratedRegex("[A-Za-z0-9_]")]
    private static partial Regex UsernameRegex();
}
