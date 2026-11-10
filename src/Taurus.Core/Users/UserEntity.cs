namespace Taurus.Core.Users;

public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Disabled;
    public string Locale { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";
    public Guid Version { get; set; }
}
