using MediatR;

namespace Taurus.Application.Features.Schemas.Approve;

public sealed record ApproveSchemaCommand(string Fingerprint) : IRequest;
