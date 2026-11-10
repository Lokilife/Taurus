namespace Taurus.Infrastructure.Configuration.Attributes;

public sealed class AttributeDefinition
{
    public string Name { get; set; } = string.Empty;
    public AttributeKind Kind { get; set; }
    public bool Multiple { get; set; }
    public bool Required { get; set; }
    public bool UserReadable { get; set; }
    public bool UserWritable { get; set; }
    public int MaximumLength { get; set; } = 256;
}
