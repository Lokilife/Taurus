using MediatR;
using Taurus.Core.Users.Attributes;

namespace Taurus.Application.Features.Schemas.ApplyCompatible;

public sealed class CheckSchemaHandler(ISchemaService service)
    : IRequestHandler<ApplyCompatibleSchemaCommand>
{
    public Task Handle(
        ApplyCompatibleSchemaCommand request,
        CancellationToken cancellationToken) => service.CheckAsync(cancellationToken);
}
