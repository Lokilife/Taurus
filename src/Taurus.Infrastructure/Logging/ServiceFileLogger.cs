using Microsoft.Extensions.Logging;

namespace Taurus.Infrastructure.Logging;

public sealed class ServiceFileLogger(
    string category,
    ServiceLogFiles files,
    ServiceLogOptions options) : ILogger
{
    private const string TrustedCategoryPrefix = "Taurus.";

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && logLevel >= options.MinimumLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        // Framework messages may contain request data; retain metadata only.
        var message = category.StartsWith(TrustedCategoryPrefix,
            StringComparison.Ordinal) ? formatter(state, null) : null;

        try
        {
            files.WriteDiagnostic(new ServiceDiagnosticRecord(DateTimeOffset.UtcNow,
                category, logLevel, eventId.Id, message, exception?.GetType().FullName));
        }
        catch (IOException)
        {
            Console.Error.WriteLine("Service log file unavailable");
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine("Service log file unavailable");
        }
    }
}
