namespace Taurus.Core.Groups;

public sealed class GroupEntity
{
    public string TenantId { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
