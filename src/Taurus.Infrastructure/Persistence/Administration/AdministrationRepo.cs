using Microsoft.EntityFrameworkCore;
using Taurus.Core.Administration;
using Taurus.Core.Users;

namespace Taurus.Infrastructure.Persistence.Administration;

public sealed class AdministrationRepo(IdentityDatabase db) : IAdministrationRepo
{
    public Task<bool> IsRootAsync(
        string tenantId,
        Guid userId,
        CancellationToken cancellationToken) => (
            from root in db.TenantRoots
            join user in db.Users on new { root.TenantId, Id = root.UserId }
                equals new { user.TenantId, user.Id }
            where root.TenantId == tenantId && root.UserId == userId
                && user.Status == UserStatus.Enabled
            select root).AnyAsync(cancellationToken);
}
