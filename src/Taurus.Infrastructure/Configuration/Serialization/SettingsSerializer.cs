using System.Text.Json;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Infrastructure.Configuration.Tenants;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Taurus.Infrastructure.Configuration.Serialization;

internal static class SettingsSerializer
{
    private const int SingleDocument = 1;
    private const string Separator = ".";

    public static Settings Load(string path)
    {
        var settings = Read<Settings>(path);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;

        foreach (var file in settings.TenantFiles)
            settings.Tenants.Add(Read<Tenant>(Path.Combine(directory, file)));

        settings.Validate();

        return settings;
    }

    public static string SerializeSchema(Tenant tenant) =>
        JsonSerializer.Serialize(tenant.Attributes);

    private static T Read<T>(string path)
    {
        var text = System.IO.File.ReadAllText(path);
        var stream = new YamlStream();

        stream.Load(new StringReader(text));

        if (stream.Documents.Count != SingleDocument)
            throw new InvalidOperationException("Expected one YAML configuration document");

        var locations = new Dictionary<Mark, string>();
        var visited = new HashSet<YamlNode>(ReferenceEqualityComparer.Instance);

        Collect(stream.Documents[default(int)].RootNode, string.Empty,
            Path.GetFullPath(path), locations, visited);

        return new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithDuplicateKeyChecking()
            .WithTypeConverter(new SecretReferenceConverter(locations))
            .Build().Deserialize<T>(text)
            ?? throw new InvalidOperationException("Empty configuration");
    }

    private static void Collect(
        YamlNode node,
        string path,
        string file,
        Dictionary<Mark, string> locations,
        HashSet<YamlNode> visited)
    {
        if (!visited.Add(node))
            return;

        locations[node.Start] = string.Concat(path,
            $" ({file}, line {node.Start.Line}, column {node.Start.Column})");

        if (node is YamlMappingNode mapping)
        {
            foreach (var pair in mapping.Children)
            {
                if (pair.Key is not YamlScalarNode key)
                    continue;

                var childPath = path.Length == default
                    ? key.Value! : string.Concat(path, Separator, key.Value);

                Collect(pair.Value, childPath, file, locations, visited);
            }
        }
        else if (node is YamlSequenceNode sequence)
        {
            for (var index = default(int); index < sequence.Children.Count; index++)
                Collect(sequence.Children[index], $"{path}[{index}]", file,
                    locations, visited);
        }
    }
}
