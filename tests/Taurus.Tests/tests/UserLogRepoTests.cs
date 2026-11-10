using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Taurus.Core.Auditing;
using Taurus.Infrastructure.Persistence.Auditing;
using Taurus.Infrastructure.Persistence.Providers.Sqlite;

namespace Taurus.Tests.tests;

public class UserLogRepoTests
{
    private const string ConnectionString = "Data Source=:memory:";
    private const string TenantId = "audit-test";

    [Theory]
    [InlineData(UserLogSeverity.None)]
    [InlineData(UserLogSeverity.Low)]
    [InlineData(UserLogSeverity.Middle)]
    [InlineData(UserLogSeverity.High)]
    public async Task SeverityRoundTripsAndCommittedEventsCannotBeChanged(
        UserLogSeverity severity)
    {
        await using var db = new SqliteDatabase(
            new DbContextOptionsBuilder<SqliteDatabase>()
                .UseSqlite(ConnectionString).Options);
        var repo = new UserLogRepo(db, NullLogger<UserLogRepo>.Instance);
        var entry = new UserLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Action = AuditAction.Login,
            Severity = severity,
            Timestamp = DateTimeOffset.UtcNow
        };

        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        repo.Add(entry);

        Assert.True(await repo.SaveAsync(AuditFailureMode.Block, default));

        db.ChangeTracker.Clear();

        var stored = await db.UserLogs.SingleAsync();

        Assert.Equal(severity, stored.Severity);

        stored.Succeeded = !stored.Succeeded;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.SaveAsync(AuditFailureMode.Block, default));
    }
}
