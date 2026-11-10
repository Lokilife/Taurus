using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Taurus.Core.Auditing;
using Taurus.Core.Sessions;
using Taurus.Core.Users;
using Taurus.Infrastructure.Persistence;
using Taurus.Infrastructure.Persistence.Auditing;
using Taurus.Infrastructure.Persistence.Authentication;
using Taurus.Infrastructure.Persistence.Providers.Sqlite;

namespace Taurus.Tests.tests;

public class StorageTests
{
    private const string ConnectionString = "Data Source=:memory:";
    private const string TenantA = "a";
    private const string TenantB = "b";
    private const string Username = "root";
    private const string Origin = "https://app.localhost";
    private const string Digest = "digest";
    private const int AbsoluteLifetimeDays = 1;
    private const int ShortIdleSeconds = 10;
    private const int RenewalSeconds = 6;
    private const int ContinuedActivitySeconds = 11;

    [Fact]
    public async Task TenantKeysPreventCrossTenantLookup()
    {
        await using var db = Create();

        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var user = new UserEntity
        {
            TenantId = TenantA,
            Id = Guid.NewGuid(),
            Username = Username,
            Status = UserStatus.Enabled
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var repo = Repo(db);
        var missing = await repo.FindUserAsync(TenantB, Username, default);
        var found = await repo.FindUserAsync(TenantA, Username, default);

        Assert.Null(missing.User);
        Assert.NotNull(found.User);
    }

    [Fact]
    public async Task SessionChecksOriginStatusExpiryAndRevocation()
    {
        await using var db = Create();

        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var user = new UserEntity
        {
            TenantId = TenantA,
            Id = Guid.NewGuid(),
            Username = Username,
            Status = UserStatus.Enabled
        };
        var now = DateTimeOffset.UtcNow;
        var lifetime = TimeSpan.FromDays(AbsoluteLifetimeDays);

        db.Users.Add(user);
        db.Sessions.Add(new SessionEntity
        {
            TenantId = TenantA,
            UserId = user.Id,
            Origin = Origin,
            TokenDigest = Digest,
            IdleExpiresAt = now + lifetime,
            AbsoluteExpiresAt = now + lifetime
        });
        await db.SaveChangesAsync();

        var repo = Repo(db);

        Assert.NotNull(await repo.GetSessionAsync(
            TenantA, Origin, Digest, now, lifetime, default));
        Assert.Null(await repo.GetSessionAsync(
            TenantB, Origin, Digest, now, lifetime, default));
        Assert.Null(await repo.GetSessionAsync(
            TenantA, string.Empty, Digest, now, lifetime, default));
        Assert.Null(await repo.GetSessionAsync(
            TenantA, Origin, Digest, now + lifetime, lifetime, default));

        user.Status = UserStatus.Disabled;
        await db.SaveChangesAsync();

        Assert.Null(await repo.GetSessionAsync(
            TenantA, Origin, Digest, now, lifetime, default));

        user.Status = UserStatus.Enabled;
        await db.SaveChangesAsync();
        await repo.RevokeSessionAsync(
            TenantA, Origin, Digest, AuditFailureMode.Block, default);

        Assert.Null(await repo.GetSessionAsync(
            TenantA, Origin, Digest, now, lifetime, default));
        Assert.Single(await db.UserLogs.ToListAsync());
    }

    [Fact]
    public async Task ShortIdleLifetimesRenewBeforeTheOriginalExpiry()
    {
        await using var db = Create();

        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA,
            Username = Username,
            Status = UserStatus.Enabled
        };
        var now = DateTimeOffset.UtcNow;
        var lifetime = TimeSpan.FromSeconds(ShortIdleSeconds);

        db.Users.Add(user);
        db.Sessions.Add(new SessionEntity
        {
            TenantId = TenantA,
            UserId = user.Id,
            Origin = Origin,
            TokenDigest = Digest,
            IdleExpiresAt = now + lifetime,
            AbsoluteExpiresAt = now + TimeSpan.FromDays(AbsoluteLifetimeDays)
        });
        await db.SaveChangesAsync();

        var repo = Repo(db);

        Assert.NotNull(await repo.GetSessionAsync(TenantA, Origin, Digest,
            now + TimeSpan.FromSeconds(RenewalSeconds), lifetime, default));
        Assert.NotNull(await repo.GetSessionAsync(TenantA, Origin, Digest,
            now + TimeSpan.FromSeconds(ContinuedActivitySeconds), lifetime, default));
    }

    private static IdentityDatabase Create() => new SqliteDatabase(
        new DbContextOptionsBuilder<SqliteDatabase>()
            .UseSqlite(ConnectionString).Options);

    private static AuthRepo Repo(IdentityDatabase db) =>
        new(db, new UserLogRepo(db, NullLogger<UserLogRepo>.Instance));
}
