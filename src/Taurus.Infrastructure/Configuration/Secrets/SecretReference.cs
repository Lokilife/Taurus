namespace Taurus.Infrastructure.Configuration.Secrets;

public sealed class SecretReference
{
    private const int SingleSource = 1;
    private const string DefaultPath = "secret reference";

    public string? Value { get; set; }
    public string? Environment { get; set; }
    public string? File { get; set; }

    /// <summary>Rejects null references with their configuration path.</summary>
    public static void Validate(SecretReference? reference, string path)
    {
        if (reference is null)
            throw new InvalidOperationException(
                $"{path}: expected a secret reference with value, environment or file");

        reference.Validate(path);
    }

    public void Validate() => Validate(DefaultPath);

    /// <summary>Validates source selection without including secret values.</summary>
    public void Validate(string path)
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
                $"{path}: specify exactly one of "
                + "value, environment or file");
    }

    public string Resolve() => Resolve(DefaultPath);

    /// <summary>Resolves a secret with a safe configuration path in failures.</summary>
    public string Resolve(string path)
    {
        Validate(path);

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
                $"{path}: cannot read the configured secret source");
        }

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"{path}: the configured secret source is missing or empty");

        return value;
    }
}
