using System.Collections;
using System.Reflection;
using Taurus.Infrastructure.Configuration.Secrets;
using YamlDotNet.Serialization.NamingConventions;

namespace Taurus.Infrastructure.Configuration;

internal static class ConfigurationContext
{
    private const string Separator = ".";

    public static void Bind(Settings settings) => Visit(settings, string.Empty,
        new HashSet<object>(ReferenceEqualityComparer.Instance));

    private static void Visit(object? value, string path, HashSet<object> ancestors)
    {
        if (value is null || value is string || value.GetType().IsValueType)
            return;

        if (!ancestors.Add(value))
            throw new InvalidOperationException($"{path}: cyclic configuration reference");

        try
        {
            if (value is IList list)
            {
                for (var index = default(int); index < list.Count; index++)
                    Visit(list[index], $"{path}[{index}]", ancestors);

                return;
            }

            if (value.GetType().Assembly != typeof(Settings).Assembly)
                return;

            foreach (var property in value.GetType().GetProperties())
            {
                if (!property.CanRead || !property.CanWrite
                    || property.GetIndexParameters().Length != default)
                    continue;

                var name = CamelCaseNamingConvention.Instance.Apply(property.Name);
                var location = path.Length == default
                    ? name : string.Concat(path, Separator, name);
                var child = property.GetValue(value);
                var secret = property.GetCustomAttribute<SecretOptionAttribute>();

                if (secret is not null)
                {
                    if (child is null && secret.Required)
                        throw new InvalidOperationException(
                            $"{location}: expected a secret reference with value, environment or file");

                    if (child is SecretReference reference)
                    {
                        // YAML aliases need independent context for each property.
                        property.SetValue(value, new SecretReference
                        {
                            Value = reference.Value,
                            Environment = reference.Environment,
                            File = reference.File,
                            ConfigurationPath = location
                        });
                    }
                }
                else
                {
                    Visit(child, location, ancestors);
                }
            }
        }
        finally
        {
            ancestors.Remove(value);
        }
    }
}
