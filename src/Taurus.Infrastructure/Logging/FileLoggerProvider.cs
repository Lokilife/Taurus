using Microsoft.Extensions.Logging;

namespace Taurus.Infrastructure.Logging;

public sealed class FileLoggerProvider(
    ServiceLogFiles files,
    ServiceLogOptions options) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        new ServiceFileLogger(categoryName, files, options);

    public void Dispose()
    {
    }
}
