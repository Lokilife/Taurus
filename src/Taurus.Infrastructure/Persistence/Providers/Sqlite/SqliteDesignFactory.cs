using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Taurus.Infrastructure.Persistence.Providers.Sqlite;

public sealed class SqliteDesignFactory : IDesignTimeDbContextFactory<SqliteDatabase>
{
    private const string DesignConnection = "Data Source=design.db";

    public SqliteDatabase CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<SqliteDatabase>()
            .UseSqlite(DesignConnection).Options);
}
