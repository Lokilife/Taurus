namespace Taurus.Infrastructure.Configuration.Secrets;

/// <summary>Marks a schema property whose secret receives a diagnostic path.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SecretOptionAttribute(bool required = true) : Attribute
{
    public bool Required { get; } = required;
}
