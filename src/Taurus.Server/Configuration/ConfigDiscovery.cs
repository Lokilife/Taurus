namespace Taurus.Server.Configuration;

public static class ConfigDiscovery
{
    private const string ConfigArgument = "--config";
    private const string SystemDirectory = "/etc/taurus";
    private const string LocalDirectory = "config";
    private const string Filename = "config.yml";
    private const int NextArgument = 1;

    public static string Resolve(ref string[] args)
    {
        string? selected = null;
        var remaining = new List<string>(args.Length);

        for (var index = default(int); index < args.Length; index++)
        {
            if (args[index] != ConfigArgument)
            {
                remaining.Add(args[index]);
                continue;
            }

            if (selected is not null || index + NextArgument >= args.Length)
                throw new InvalidOperationException("Provide --config once with a path");

            selected = Path.GetFullPath(args[++index]);
        }

        args = remaining.ToArray();

        if (selected is not null)
            return selected;

        var systemPath = Path.Combine(SystemDirectory, Filename);

        if (File.Exists(systemPath))
            return systemPath;

        var localPath = Path.Combine(AppContext.BaseDirectory, LocalDirectory, Filename);

        if (File.Exists(localPath))
            return localPath;

        throw new FileNotFoundException("No Taurus configuration; use --config PATH");
    }
}
