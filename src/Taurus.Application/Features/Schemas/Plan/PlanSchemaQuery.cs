using MediatR;

namespace Taurus.Application.Features.Schemas.Plan;

public sealed record PlanSchemaQuery : IRequest<string>;
