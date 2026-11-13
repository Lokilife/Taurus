using Microsoft.EntityFrameworkCore;
using Taurus.Core.Groups;
using Taurus.Core.Users;
using Taurus.Core.Users.Profiles;

namespace Taurus.Infrastructure.Persistence.Users;

public sealed class UserProfileRepo(IdentityDatabase db) : IUserProfileRepo
{
    public async Task<UserProfile?> FindAsync(
        string tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Where(
                x => x.TenantId == tenantId && x.Id == userId
                    && x.Status == UserStatus.Enabled)
            .Select(x => new { x.Id, x.Username, x.Locale, x.Timezone, x.Root })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        var groups = await (
            from membership in db.Set<GroupMembershipEntity>().AsNoTracking()
            join userGroup in db.Set<GroupEntity>().AsNoTracking()
                on new { membership.TenantId, Id = membership.GroupId }
                equals new { userGroup.TenantId, userGroup.Id }
            where membership.TenantId == tenantId && membership.UserId == userId
            orderby userGroup.Name, userGroup.Id
            select new UserGroup(userGroup.Id, userGroup.Name))
            .ToArrayAsync(cancellationToken);

        return new UserProfile(user.Id, user.Username, user.Locale, user.Timezone,
            user.Root, groups);
    }
}
