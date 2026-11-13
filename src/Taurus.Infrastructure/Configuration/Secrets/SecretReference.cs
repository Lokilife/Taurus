using YamlDotNet.Serialization;

namespace Taurus.Infrastructure.Configuration.Secrets;

public sealed class SecretReference
{
    private const int SingleSource = 1;
    private const string DefaultPath = "secret reference";

    public string? Value { get; set; }
    public string? Environment { get; set; }
    public string? File { get; set; }

    [YamlIgnore]
    internal string ConfigurationPath { get; init; } = DefaultPath;

    /// <summary>Validates source selection without including secret values.</summary>
    public void Validate()
    {
        var sources = default(int);

        if (Value is not null)
            sources++;

        if (!string.IsNullOrWhiteSpace(Environment))
            sources++;

        if (!string.IsNullOrWhiteSpace(File))
            sources++;

        if (sources != SingleSource)
            throw new InvalidOperationException(
                $"{ConfigurationPath}: specify exactly one of value, environment or file");
    }

    /// <summary>Resolves a secret with its schema-bound path in failures.</summary>
    public string Resolve()
    {
        Validate();

        string? value;

        try
        {
            value = Value ?? (!string.IsNullOrWhiteSpace(Environment)
                ? System.Environment.GetEnvironmentVariable(Environment)
                : System.IO.File.ReadAllText(File!).TrimEnd('\r', '\n'));
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new InvalidOperationException(
                $"{ConfigurationPath}: cannot read the configured secret source");
        }

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"{ConfigurationPath}: the configured secret source is missing or empty");

        return value;
    }
}
