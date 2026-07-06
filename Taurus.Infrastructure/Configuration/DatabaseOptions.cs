using System.Diagnostics.CodeAnalysis;

namespace Taurus.Infrastructure.Configuration;

public enum DatabaseProvider
{
    Sqlite,
    PostgreSQL,
}

public record DatabaseOptions
{
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;
    public string? Host { get; set; } = null;
    public int? Port { get; set; } = null;
    public string? Database { get; set; } = null;
    public string? Username { get; set; } = null;
    public string? Password { get; set; } = null;
    public bool UseSSL { get; set; } = false;
    public string? Path { get; set; } = "./app.db";

    [MemberNotNullWhen(true, nameof(Host), nameof(Port), nameof(Database), nameof(Username), nameof(Password))]
    public bool IsConnection => Provider != DatabaseProvider.Sqlite && !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(Database) && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    [MemberNotNullWhen(true, nameof(Path))]
    public bool IsFile => Provider == DatabaseProvider.Sqlite && !string.IsNullOrWhiteSpace(Path);

    public string ConnectionString
    {
        get
        {
            if (IsConnection)
                return $"Host={Host};Port={Port ?? 5432};Database={Database};Username={Username};Password={Password};SSL Mode={(UseSSL ? "Require" : "Disable")};Trust Server Certificate={(UseSSL ? "true" : "false")}";
            else if (IsFile)
                return $"Data Source={Path}";
            else
                throw new InvalidOperationException("Invalid database provider.");
        }
    }

    public DatabaseOptions(DatabaseProvider provider = DatabaseProvider.Sqlite, string? host = null, int? port = null, string? database = null, string? username = null, string? password = null, bool useSSL = false, string? path = "./app.db")
    {
        Provider = provider;
        Host = host;
        Port = port;
        Database = database;
        Username = username;
        Password = password;
        UseSSL = useSSL;
        Path = path;
    }

    public bool Validate([NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;

        if (IsConnection)
            return true;
        else if (IsFile)
            return true;
        else
        {
            errorMessage = "Invalid database configuration. Please check the provided options.";
            return false;
        }
    }
}
