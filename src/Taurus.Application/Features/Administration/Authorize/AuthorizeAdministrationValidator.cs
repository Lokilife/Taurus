using FluentValidation;
using Taurus.Application.Features.Sessions;

namespace Taurus.Application.Features.Administration.Authorize;

public sealed class AuthorizeAdministrationValidator
    : AbstractValidator<AuthorizeAdministrationQuery>
{
    private const int MaximumTenantIdentifierLength = 128;

    public AuthorizeAdministrationValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty()
            .MaximumLength(MaximumTenantIdentifierLength);
        RuleFor(x => x.Origin).NotEmpty();
        RuleFor(x => x.Token).NotNull()
            .MaximumLength(SessionTokens.MaximumTokenCharacters);
    }
}
