namespace Taurus.Infrastructure.Configuration.Secrets;

public sealed class SecretReference
{
    private const int SingleSource = 1;

    public string? Value { get; set; }
    public string? Environment { get; set; }
    public string? File { get; set; }

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
            throw new InvalidOperationException("Provide exactly one secret source");
    }

    public string Resolve()
    {
        Validate();

        var value = Value ?? (!string.IsNullOrWhiteSpace(Environment)
            ? System.Environment.GetEnvironmentVariable(Environment)
            : System.IO.File.ReadAllText(File!).TrimEnd('\r', '\n'));

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Secret reference is unresolved");

        return value;
    }
}
