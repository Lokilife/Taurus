using MediatR;
using Taurus.Core.Users.Attributes;

namespace Taurus.Application.Features.Schemas.Approve;

public sealed class ApproveSchemaHandler(ISchemaService service)
    : IRequestHandler<ApproveSchemaCommand>
{
    public Task Handle(
        ApproveSchemaCommand request,
        CancellationToken cancellationToken) => service.ApproveAsync(
            request.Fingerprint, cancellationToken);
}
