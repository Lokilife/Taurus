using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Taurus.Infrastructure.Persistence;

public sealed class PostgresDbContext : TaurusDbContext
{
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach(var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach(var property in entity.GetProperties())
            {
                if (property.FieldInfo?.FieldType == typeof(DateTime) || property.FieldInfo?.FieldType == typeof(DateTime?))
                    property.SetColumnType("timestamp with time zone");
            }
        }
    }

    protected override Migration[] Migrations => [
        new Migrations.Postgres.Initial(),
    ];
}
