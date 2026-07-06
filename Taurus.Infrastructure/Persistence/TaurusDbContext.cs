using System.Reflection;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Taurus.Infrastructure.Persistence;

public abstract class TaurusDbContext : DbContext
{
    #region Models
    // We have workaround the issue using the compiled models in DependencyInjection.cs
    // so we can disable the warning for the constructor
#pragma warning disable IL2026
#pragma warning disable IL3050
    public TaurusDbContext(DbContextOptions options) : base(options)
#pragma warning restore IL2026 
#pragma warning restore IL3050 
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    // public DbSet<UserAttributeEntity> UserAttributes => Set<UserAttributeEntity>();
    public DbSet<CredentialEntity> Credentials => Set<CredentialEntity>();
    public DbSet<GroupEntity> Groups => Set<GroupEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    #endregion

    #region Migrations

    protected abstract Migration[] Migrations { get; }

    /// <summary>
    /// Hacky solution to apply migrations due to limitations of EF Core in NativeAOT context
    /// </summary>
    public async Task TaurusMigrate()
    {
        var sqlGenerator = this.GetService<IMigrationsSqlGenerator>();
        var connection = this.GetService<IRelationalConnection>();
        var annotationProvider = this.GetService<IMigrationsAnnotationProvider>();
        var typeMapper = this.GetService<IRelationalTypeMappingSource>();
        var logger = this.GetService<ILogger<TaurusDbContext>>();

        var sortedMigrations = Migrations
            .OrderBy(m => m.GetType().Name)
            .ToList();

        await connection.OpenAsync(default);

        await EnsureHistoryTableExistsAsync(connection);

        var appliedIds = new HashSet<string>();
        using (var cmd = connection.DbConnection.CreateCommand())
        {
            cmd.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                appliedIds.Add(reader.GetString(0));
            }
        }

        foreach (var migration in sortedMigrations)
        {
            var migrationId = migration.GetType().Name;

            if (appliedIds.Contains(migrationId))
                continue;

            logger.LogInformation("Found pending migration {MigrationId}, applying", migrationId);

            var builder = new MigrationBuilder(null);
            var operations = migration.UpOperations;

            if (!operations.Any())
                continue;

            var commands = sqlGenerator.Generate(operations, Model);

            foreach (var command in commands)
            {
                using var dbCmd = connection.DbConnection.CreateCommand();
                dbCmd.CommandText = command.CommandText;
                logger.LogTrace("{CommandText}", command.CommandText);
                await dbCmd.ExecuteNonQueryAsync();
            }

            using var insertCmd = connection.DbConnection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
                VALUES (@id, @version)";

            var versionParam = insertCmd.CreateParameter();
            versionParam.ParameterName = "@version";
            versionParam.Value = typeof(TaurusDbContext).Assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()?.InformationalVersion ?? "10.0.0";

            var idParam = insertCmd.CreateParameter();
            idParam.ParameterName = "@id";
            idParam.Value = migrationId;

            insertCmd.Parameters.Add(idParam);
            insertCmd.Parameters.Add(versionParam);
            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task EnsureHistoryTableExistsAsync(IRelationalConnection connection)
    {
        try
        {
            // Try to read from the table – if it exists, this succeeds.
            using var cmd = connection.DbConnection.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM __EFMigrationsHistory LIMIT 1";
            await cmd.ExecuteScalarAsync();
        }
        catch
        {
            // Table doesn't exist – create it.
            using var cmd = connection.DbConnection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE __EFMigrationsHistory (
                    MigrationId TEXT NOT NULL PRIMARY KEY,
                    ProductVersion TEXT NOT NULL
                )";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    #endregion
}
