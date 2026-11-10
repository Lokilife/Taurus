namespace Taurus.Core.Auditing;

/// <summary>Security significance of a user-audit event.</summary>
public enum UserLogSeverity
{
    /// <summary>Informational activity without meaningful consequences.</summary>
    None = 0,
    /// <summary>Consequential activity of limited investigative importance.</summary>
    Low = 1,
    /// <summary>Activity relevant to security investigations.</summary>
    Middle = 2,
    /// <summary>Sensitive changes with potentially irreversible consequences.</summary>
    High = 3
}
