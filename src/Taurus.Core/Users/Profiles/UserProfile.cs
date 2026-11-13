namespace Taurus.Core.Users.Profiles;

/// <summary>Credential-free user data for application profile queries.</summary>
public sealed record UserProfile(
    Guid Id,
    string Username,
    string Locale,
    string Timezone,
    bool Root,
    IReadOnlyList<UserGroup> Groups);
