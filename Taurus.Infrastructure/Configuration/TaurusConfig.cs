using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

namespace Taurus.Infrastructure.Configuration;

public sealed class TaurusConfig
{
    public DatabaseOptions Database { get; set; }

    public TaurusConfig(DatabaseOptions database)
    {
        Database = database;
    }

    public bool Validate([NotNullWhen(false)] out string? message)
    {
        message = null;

        if (Database is null)
        {
            message = "Database configuration is missing.";
            return false;
        }

        if (!Database.Validate(out message))
            return false;

        return true;
    }
}
