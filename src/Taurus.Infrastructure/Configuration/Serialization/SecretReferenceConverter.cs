using Taurus.Infrastructure.Configuration.Secrets;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Taurus.Infrastructure.Configuration.Serialization;

internal sealed class SecretReferenceConverter(
    IReadOnlyDictionary<Mark, string> locations) : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(SecretReference);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer deserialize)
    {
        var mark = parser.Current!.Start;
        var location = locations[mark];
        SecretReferenceData? data;

        try
        {
            data = deserialize(typeof(SecretReferenceData)) as SecretReferenceData;
        }
        catch (YamlException)
        {
            throw new InvalidOperationException(
                $"{location}: expected a secret reference with value, environment or file");
        }

        if (data is null)
            throw new InvalidOperationException(
                $"{location}: expected a secret reference with value, environment or file");

        return new SecretReference
        {
            Value = data.Value,
            Environment = data.Environment,
            File = data.File,
            ConfigurationPath = location
        };
    }

    public void WriteYaml(
        IEmitter emitter,
        object? value,
        Type type,
        ObjectSerializer serialize) => throw new NotSupportedException(
            "The configuration loader does not serialize secret references");
}
