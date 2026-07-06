using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Taurus.Infrastructure.Persistence;

public sealed class SqliteDbContext : TaurusDbContext
{
    public SqliteDbContext(DbContextOptions<SqliteDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.ConfigureWarnings(x =>
        {
            x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning);
#if DEBUG
            // for tests
            x.Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning);
#endif
        });

#if DEBUG
        options.EnableSensitiveDataLogging();
#endif
    }

    protected override Migration[] Migrations => [
        new Migrations.Sqlite.Initial(),
    ];
}
