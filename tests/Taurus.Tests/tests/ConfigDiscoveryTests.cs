using Taurus.Server.Configuration;

namespace Taurus.Tests.tests;

public class ConfigDiscoveryTests
{
    private const string ConfigOption = "--config";
    private const string Command = "migrate";
    private const string Filename = "explicit-config.yml";

    [Fact]
    public void ExplicitPathOverridesDiscoveryAndPreservesCommand()
    {
        string[] args = [Command, ConfigOption, Filename];

        var selected = ConfigDiscovery.Resolve(ref args);

        Assert.Equal(Path.GetFullPath(Filename), selected);
        Assert.Equal(new[] { Command }, args);
    }

    [Fact]
    public void MissingOrDuplicateConfigArgumentsAreRejected()
    {
        string[] missing = [ConfigOption];
        string[] duplicate = [ConfigOption, Filename, ConfigOption, Filename];

        Assert.Throws<InvalidOperationException>(() =>
            ConfigDiscovery.Resolve(ref missing));
        Assert.Throws<InvalidOperationException>(() =>
            ConfigDiscovery.Resolve(ref duplicate));
    }
}
