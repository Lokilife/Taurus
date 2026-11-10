namespace Taurus.Tests;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    private const string EnvironmentName = "TAURUS_TEST_POSTGRESQL";

    public PostgreSqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(EnvironmentName) is null)
            Skip = "Set TAURUS_TEST_POSTGRESQL to a disposable database";
    }
}
