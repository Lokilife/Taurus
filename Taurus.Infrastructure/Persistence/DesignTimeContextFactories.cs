using Microsoft.EntityFrameworkCore.Design;
using SQLitePCL;

namespace Taurus.Infrastructure.Persistence;

public sealed class DesignTimeContextFactoryPostgres : IDesignTimeDbContextFactory<PostgresDbContext>
{
    public PostgresDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgresDbContext>();
        optionsBuilder.UseNpgsql("Server=localhost");
        return new PostgresDbContext(optionsBuilder.Options);
    }
}

public sealed class DesignTimeContextFactorySqlite : IDesignTimeDbContextFactory<SqliteDbContext>
{
    public SqliteDbContext CreateDbContext(string[] args)
    {
        raw.SetProvider(new SQLite3Provider_e_sqlite3());

        var optionsBuilder = new DbContextOptionsBuilder<SqliteDbContext>();
        optionsBuilder.UseSqlite("Data Source=:memory:");
        return new SqliteDbContext(optionsBuilder.Options);
    }
}

