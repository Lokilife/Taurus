using Microsoft.Extensions.Logging;

namespace Taurus.Infrastructure.Logging;

public sealed class ServiceLogOptions
{
    private const long DefaultMaximumFileBytes = 10 * 1024 * 1024;

    public string Directory { get; set; } = "logs";
    public long MaximumFileBytes { get; set; } = DefaultMaximumFileBytes;
    public int RetainedFiles { get; set; } = 3;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}
