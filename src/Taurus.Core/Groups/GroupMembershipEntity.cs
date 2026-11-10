namespace Taurus.Core.Groups;

public sealed class GroupMembershipEntity
{
    public string TenantId { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
}
