using Taurus.Core.Users.Profiles;

namespace Taurus.Application.Features.Users.CurrentUser;

/// <summary>Public user data and effective administration capability.</summary>
public sealed record CurrentUserProfile(UserProfile User, bool CanManageUsers);
