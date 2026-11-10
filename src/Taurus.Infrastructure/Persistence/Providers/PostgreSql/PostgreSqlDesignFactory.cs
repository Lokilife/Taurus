using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Taurus.Infrastructure.Persistence.Providers.PostgreSql;

public sealed class PostgreSqlDesignFactory
    : IDesignTimeDbContextFactory<PostgreSqlDatabase>
{
    private const string DesignConnection =
        "Host=localhost;Database=taurus;Username=taurus";

    public PostgreSqlDatabase CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<PostgreSqlDatabase>()
            .UseNpgsql(DesignConnection).Options);
}
