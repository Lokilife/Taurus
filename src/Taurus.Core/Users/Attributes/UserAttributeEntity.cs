namespace Taurus.Core.Users.Attributes;

public sealed class UserAttributeEntity
{
    public string TenantId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public string Value { get; set; } = string.Empty;
}
