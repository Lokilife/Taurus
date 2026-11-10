using FluentValidation;

namespace Taurus.Application.Features.Schemas.Approve;

public sealed class ApproveSchemaValidator : AbstractValidator<ApproveSchemaCommand>
{
    private const int FingerprintLength = 64;

    public ApproveSchemaValidator()
    {
        RuleFor(x => x.Fingerprint).NotEmpty().Length(FingerprintLength);
    }
}
