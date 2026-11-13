namespace Taurus.Core.Administration;

/// <summary>Tenant-local binding to the original bootstrap administrator.</summary>
public sealed class TenantRootEntity
{
    public string TenantId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}
