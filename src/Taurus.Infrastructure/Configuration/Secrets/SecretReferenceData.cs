namespace Taurus.Infrastructure.Configuration.Secrets;

/// <summary>Serialized source selection without runtime context.</summary>
internal sealed class SecretReferenceData
{
    public string? Value { get; set; }
    public string? Environment { get; set; }
    public string? File { get; set; }
}
