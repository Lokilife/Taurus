using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Taurus.Core.Auditing;
using Taurus.Core.Credentials;
using Taurus.Core.Sessions;
using Taurus.Core.Users;
using Taurus.Infrastructure.Configuration.Tenants;
using Taurus.Infrastructure.Persistence;
using Taurus.Infrastructure.Persistence.Auditing;
using Taurus.Infrastructure.Persistence.Authentication;
using Taurus.Infrastructure.Persistence.Providers.PostgreSql;
using Taurus.Infrastructure.Persistence.Providers.Sqlite;

namespace Taurus.Tests.tests;

public class DatabaseContractTests
{
    private const string ConnectionString = "Data Source=:memory:";
    private const string PostgreSqlEnvironment = "TAURUS_TEST_POSTGRESQL";
    private const string Tenant = "contract";
    private const string OtherTenant = "other";
    private const string Username = "root";
    private const string Digest = "test-digest";
    private const string Origin = "https://app.localhost";
    private const string MissingAuditTableSql = "DROP TABLE \"UserLogs\"";

    [Fact]
    public Task SqliteContract() => MigrationsConstraintsConcurrencyAndAudit(false);

    [PostgreSqlFact]
    public Task PostgreSqlContract() => MigrationsConstraintsConcurrencyAndAudit(true);

    private static async Task MigrationsConstraintsConcurrencyAndAudit(bool postgres)
    {
        var connection = Environment.GetEnvironmentVariable(PostgreSqlEnvironment);

        if (postgres && connection is null)
            throw new InvalidOperationException(
                "Set TAURUS_TEST_POSTGRESQL to a disposable PostgreSQL database");

        await using var db = postgres
            ? (IdentityDatabase)new PostgreSqlDatabase(
                new DbContextOptionsBuilder<PostgreSqlDatabase>()
                    .UseNpgsql(connection).Options)
            : new SqliteDatabase(new DbContextOptionsBuilder<SqliteDatabase>()
                .UseSqlite(ConnectionString).Options);

        if (postgres)
            await db.Database.EnsureDeletedAsync();
        else
            await db.Database.OpenConnectionAsync();

        await db.Database.MigrateAsync();

        Assert.False(db.Database.HasPendingModelChanges());

        var user = new UserEntity
        {
            TenantId = Tenant,
            Id = Guid.NewGuid(),
            Username = Username,
            Status = UserStatus.Enabled,
            Version = Guid.NewGuid()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var stale = await db.Users.SingleAsync();

        await db.Users.ExecuteUpdateAsync(update =>
            update.SetProperty(x => x.Version, Guid.NewGuid()));

        stale.Status = UserStatus.Disabled;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            db.SaveChangesAsync());

        db.ChangeTracker.Clear();
        db.Users.Add(new UserEntity
        {
            TenantId = Tenant,
            Id = Guid.NewGuid(),
            Username = Username,
            Status = UserStatus.Enabled
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        db.ChangeTracker.Clear();
        db.Credentials.Add(new CredentialEntity
        {
            TenantId = OtherTenant,
            Id = Guid.NewGuid(),
            UserId = user.Id
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        db.ChangeTracker.Clear();
        await db.Database.ExecuteSqlRawAsync(MissingAuditTableSql);

        var repo = new AuthRepo(db,
            new UserLogRepo(db, NullLogger<UserLogRepo>.Instance));
        var session = new SessionEntity
        {
            TenantId = Tenant,
            UserId = user.Id,
            TokenDigest = Digest,
            Origin = Origin
        };
        var audit = new UserLogEntity
        {
            TenantId = Tenant,
            Id = Guid.NewGuid(),
            Action = AuditAction.Login,
            Severity = UserLogSeverity.Middle
        };

        Assert.False(await repo.CreateSessionAsync(
            session, audit, AuditFailureMode.Block, default));
        Assert.Empty(await db.Sessions.AsNoTracking().ToListAsync());

        db.ChangeTracker.Clear();

        Assert.True(await repo.CreateSessionAsync(
            session, audit, AuditFailureMode.Allow, default));
        Assert.Single(await db.Sessions.AsNoTracking().ToListAsync());

        if (postgres)
            await db.Database.EnsureDeletedAsync();
    }
}
