using Microsoft.EntityFrameworkCore;
using Taurus.Infrastructure.Persistence;

namespace Taurus.Infrastructure.Persistence.Providers.Sqlite;

public sealed class SqliteDatabase(DbContextOptions<SqliteDatabase> options)
    : IdentityDatabase(options);
