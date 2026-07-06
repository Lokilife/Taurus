namespace Taurus.Core.Entities;

public sealed class GroupEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty; // used only for web-UI
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // EF mapped collection properties
    public ICollection<UserEntity> Users { get; set; } = null!;
}
