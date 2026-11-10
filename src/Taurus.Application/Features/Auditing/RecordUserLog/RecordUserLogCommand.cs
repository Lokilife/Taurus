using MediatR;
using Taurus.Core.Auditing;

namespace Taurus.Application.Features.Auditing.RecordUserLog;

/// <summary>Persists a security event under the configured failure policy.</summary>
public sealed record RecordUserLogCommand(
    UserLogEntity Entry,
    AuditFailureMode FailureMode) : IRequest<bool>;
