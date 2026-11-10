using Microsoft.Extensions.Logging;

namespace Taurus.Infrastructure.Logging;

/// <summary>Operational metadata without exception messages or request objects.</summary>
public sealed record ServiceDiagnosticRecord(
    DateTimeOffset Timestamp,
    string Category,
    LogLevel Level,
    int EventId,
    string? Message,
    string? ExceptionType);
