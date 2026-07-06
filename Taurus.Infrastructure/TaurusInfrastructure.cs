using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Taurus.Core.Interfaces;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Persistence;
using Taurus.Infrastructure.Persistence.CompiledModels.Postgres;
using Taurus.Infrastructure.Persistence.CompiledModels.Sqlite;
using Taurus.Infrastructure.Persistence.Repositories;

namespace Taurus.Infrastructure;

public static class TaurusInfrastructure
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, ConfigurationManager configuration)
    {
        var section = configuration.GetRequiredSection("App");
        var config = section.Get<TaurusConfig>();
        services.Configure<TaurusConfig>(section);

        if (config is null)
        {
            throw new InvalidOperationException("Failed to bind configuration section 'app' to TaurusConfig.");
        }

        if (!config.Validate(out var message))
        {
            throw new InvalidOperationException(message);
        }

        services.AddSingleton<TaurusConfig>(config);

        if (config.Database.Provider == DatabaseProvider.PostgreSQL)
        {
            services.AddDbContext<TaurusDbContext, PostgresDbContext>(options =>
                options.UseNpgsql(config.Database.ConnectionString)
                       .UseModel(PostgresDbContextModel.Instance));
        }
        else if (config.Database.Provider == DatabaseProvider.Sqlite)
        {
            services.AddDbContext<TaurusDbContext, SqliteDbContext>(options =>
                options.UseSqlite(config.Database.ConnectionString)
                       .UseModel(SqliteDbContextModel.Instance));
        }
        else
        {
            throw new InvalidOperationException("Invalid database provider.");
        }

        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<ICredentialRepo, CredentialRepo>();
        services.AddScoped<IGroupRepo, GroupRepo>();

        return services;
    }
}
