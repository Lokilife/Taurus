using Microsoft.EntityFrameworkCore;
using Taurus.Infrastructure.Persistence.Providers.Sqlite;

namespace Taurus.Tests;

public static class TestDatabase
{
    public static async Task<SqliteDatabase> CreateAsync()
    {
        var db = new SqliteDatabase(new DbContextOptionsBuilder<SqliteDatabase>()
            .UseSqlite(TestSettings.ConnectionString).Options);

        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        return db;
    }
}
