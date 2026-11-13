using FluentValidation;
using Taurus.Application.Features.Sessions;

namespace Taurus.Application.Features.Users.CurrentUser;

public sealed class CurrentUserValidator : AbstractValidator<CurrentUserQuery>
{
    private const int MaximumTenantIdentifierLength = 128;

    public CurrentUserValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty()
            .MaximumLength(MaximumTenantIdentifierLength);
        RuleFor(x => x.Origin).NotEmpty();
        RuleFor(x => x.Token).NotNull()
            .MaximumLength(SessionTokens.MaximumTokenCharacters);
    }
}
