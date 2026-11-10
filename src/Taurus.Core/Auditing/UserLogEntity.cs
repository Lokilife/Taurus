namespace Taurus.Core.Auditing;

public sealed class UserLogEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public AuditAction Action { get; set; }
    public required UserLogSeverity Severity { get; set; }
    public bool Succeeded { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
