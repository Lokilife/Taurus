using System.Text.Json;

namespace Taurus.Infrastructure.Logging;

public sealed class ServiceLogFiles : IDisposable
{
    private const string AccessFile = "access.log";
    private const string DiagnosticFile = "service.log";
    private readonly RotatingLogFile access;
    private readonly RotatingLogFile diagnostic;

    public ServiceLogFiles(ServiceLogOptions options)
    {
        access = new RotatingLogFile(
            Path.Combine(options.Directory, AccessFile), options);
        diagnostic = new RotatingLogFile(
            Path.Combine(options.Directory, DiagnosticFile), options);
    }

    public void WriteAccess(ServiceAccessRecord record) =>
        access.Write(JsonSerializer.Serialize(record));

    public void WriteDiagnostic(ServiceDiagnosticRecord record) =>
        diagnostic.Write(JsonSerializer.Serialize(record));

    public void Dispose()
    {
        access.Dispose();
        diagnostic.Dispose();
    }
}
