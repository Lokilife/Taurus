using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taurus.Core.Auditing;
using Taurus.Infrastructure.Persistence;

namespace Taurus.Infrastructure.Persistence.Auditing;

public sealed class UserLogRepo(
    IdentityDatabase db,
    ILogger<UserLogRepo> logger) : IUserLogRepo
{
    public void Add(UserLogEntity entry)
    {
        if (!Enum.IsDefined(entry.Severity))
            throw new ArgumentOutOfRangeException(nameof(entry));

        db.UserLogs.Add(entry);
    }

    public async Task<bool> SaveAsync(
        AuditFailureMode failureMode,
        CancellationToken cancellationToken)
    {
        foreach (var entry in db.ChangeTracker.Entries<UserLogEntity>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("User audit records are append-only");
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateException)
        {
            logger.LogError("User audit persistence failed");

            if (failureMode == AuditFailureMode.Block)
                return false;

            var pending = new List<UserLogEntity>();

            foreach (var entry in db.ChangeTracker.Entries<UserLogEntity>())
            {
                if (entry.State == EntityState.Added)
                    pending.Add(entry.Entity);
            }

            foreach (var entry in pending)
                db.Entry(entry).State = EntityState.Detached;

            try
            {
                await db.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }
    }
}
