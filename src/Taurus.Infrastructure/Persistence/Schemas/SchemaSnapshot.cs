namespace Taurus.Infrastructure.Persistence.Schemas;

public sealed class SchemaSnapshot
{
    public string TenantId { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public Guid Version { get; set; }
}
