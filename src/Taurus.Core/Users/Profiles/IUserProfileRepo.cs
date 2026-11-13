namespace Taurus.Core.Users.Profiles;

public interface IUserProfileRepo
{
    /// <summary>Loads enabled-user public data and tenant-scoped memberships.</summary>
    Task<UserProfile?> FindAsync(
        string tenantId,
        Guid userId,
        CancellationToken cancellationToken);
}
