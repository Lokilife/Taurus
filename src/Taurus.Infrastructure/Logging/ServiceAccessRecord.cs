namespace Taurus.Infrastructure.Logging;

/// <summary>Allowlisted access metadata; never includes raw URLs or headers.</summary>
public sealed record ServiceAccessRecord(
    DateTimeOffset Timestamp,
    string Endpoint,
    string IpAddress,
    string Protocol,
    int StatusCode);
