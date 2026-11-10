using Microsoft.EntityFrameworkCore;
using Taurus.Infrastructure.Persistence;

namespace Taurus.Infrastructure.Persistence.Providers.PostgreSql;

public sealed class PostgreSqlDatabase(DbContextOptions<PostgreSqlDatabase> options)
    : IdentityDatabase(options);
