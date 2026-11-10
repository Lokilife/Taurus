using MediatR;
using Taurus.Core.Users.Attributes;

namespace Taurus.Application.Features.Schemas.Plan;

public sealed class PlanSchemaHandler(ISchemaService service)
    : IRequestHandler<PlanSchemaQuery, string>
{
    public Task<string> Handle(
        PlanSchemaQuery request,
        CancellationToken cancellationToken) => service.PlanAsync(cancellationToken);
}
