using Microsoft.EntityFrameworkCore;
using Taurus.Core.Administration;
using Taurus.Core.Users;

namespace Taurus.Infrastructure.Persistence.Administration;

public sealed class AdministrationRepo(IdentityDatabase db) : IAdministrationRepo
{
    public Task<bool> IsRootAsync(
        string tenantId,
        Guid userId,
        CancellationToken cancellationToken) => db.Users.AnyAsync(
            x => x.TenantId == tenantId && x.Id == userId && x.Root
                && x.Status == UserStatus.Enabled, cancellationToken);
}
