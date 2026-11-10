namespace Taurus.Core.Sessions;

public sealed class SessionEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public string TokenDigest { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset IdleExpiresAt { get; set; }
    public DateTimeOffset AbsoluteExpiresAt { get; set; }
    public bool Revoked { get; set; }
}
