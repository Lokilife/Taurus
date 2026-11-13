using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Taurus.Core.Administration;
using Taurus.Core.Auditing;
using Taurus.Core.Credentials;
using Taurus.Core.Sessions;
using Taurus.Core.Users;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Infrastructure.Configuration.Tenants;
using Taurus.Infrastructure.Authentication.Keys;
using Taurus.Infrastructure.Authentication.Opaque;
using Taurus.Infrastructure.Provisioning;
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
    private const string InitialMigration = "Initial";
    private const string LegacyTenant = "legacy";
    private const string LegacyPassword = "legacy-bootstrap-test-password";
    private const string DefaultLocale = "en";
    private const string DefaultTimezone = "UTC";
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

        await VerifyLegacyMigrationAsync(db);

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

        var stale = await db.Users.SingleAsync(x => x.TenantId == Tenant);

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
        db.TenantRoots.Add(new TenantRootEntity
        {
            TenantId = OtherTenant,
            UserId = user.Id
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        db.ChangeTracker.Clear();

        var legacyRoot = await db.Users.SingleAsync(x => x.TenantId == LegacyTenant);

        db.Users.Remove(legacyRoot);

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

    private static async Task VerifyLegacyMigrationAsync(IdentityDatabase db)
    {
        var userId = Guid.NewGuid();
        var version = Guid.NewGuid();
        var auditId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow.UtcTicks;
        var action = nameof(AuditAction.Bootstrap);

        await db.GetService<IMigrator>().MigrateAsync(InitialMigration);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Users"
                ("TenantId", "Id", "Username", "Status", "Locale", "Timezone", "Version")
            VALUES ({LegacyTenant}, {userId}, {Username}, {(int)UserStatus.Enabled},
                {DefaultLocale}, {DefaultTimezone}, {version})
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "UserLogs"
                ("TenantId", "Id", "ActorId", "Action", "Severity",
                    "Succeeded", "Timestamp")
            VALUES ({LegacyTenant}, {auditId}, {userId}, {action},
                {(int)UserLogSeverity.High}, {true}, {timestamp})
            """);
        await db.Database.MigrateAsync();

        var settings = new Settings
        {
            Tenants = [new Tenant
            {
                Id = LegacyTenant,
                Issuer = Origin,
                ApplicationOrigins = [Origin],
                RootPassword = new SecretReference { Value = LegacyPassword }
            }]
        };
        var opaque = new OpaqueService(new OpaqueOptions
        {
            Setup = new SecretReference { Value = KeyBootstrap.GenerateOpaqueSetup() }
        }, TimeProvider.System);
        var bootstrap = new Bootstrapper(db, settings, opaque,
            new UserLogRepo(db, NullLogger<UserLogRepo>.Instance));

        await bootstrap.RunAsync(default);

        Assert.Equal(userId, (await db.TenantRoots.SingleAsync()).UserId);
        Assert.Equal(version, (await db.Users.SingleAsync()).Version);
        Assert.Null((await db.UserLogs.SingleAsync(x => x.Id == auditId)).TargetUserId);
        db.ChangeTracker.Clear();
    }
}
