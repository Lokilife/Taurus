namespace Taurus.Core.Auditing;

/// <summary>Append-only audit persistence in the current security unit of work.</summary>
public interface IUserLogRepo
{
    /// <summary>Stages an event without updating or deleting persisted events.</summary>
    void Add(UserLogEntity entry);

    /// <summary>
    /// Saves staged events and pending security changes atomically. Allow mode
    /// may commit security changes without events if audit persistence fails.
    /// </summary>
    Task<bool> SaveAsync(
        AuditFailureMode failureMode,
        CancellationToken cancellationToken);
}
