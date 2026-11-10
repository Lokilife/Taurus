using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Taurus.Infrastructure.Logging;
using Taurus.Server.Auditing;

namespace Taurus.Tests.tests;

public class FileLoggingTests
{
    private const string AccessFile = "access.log";
    private const string DiagnosticFile = "service.log";
    private const string EndpointName = "known-endpoint";
    private const string Secret = "must-not-appear-in-logs";
    private const string SafeMessage = "Safe diagnostic";
    private const string FrameworkCategory = "Microsoft.AspNetCore.Test";
    private const string ApplicationCategory = "Taurus.Tests.Logging";
    private const string HttpProtocol = "HTTP";
    private const string RotationPattern = "access.log*";
    private const long MaximumFileBytes = 512;
    private const int RetainedFiles = 2;
    private const int TotalFiles = RetainedFiles + 1;
    private const int RecordCount = 20;

    [Fact]
    public async Task AccessLogsDoNotNeedADatabaseOrContainRawRequestData()
    {
        var directory = TempDirectory();

        try
        {
            using var files = new ServiceLogFiles(new ServiceLogOptions
            {
                Directory = directory
            });
            using var services = new ServiceCollection()
                .AddSingleton(files).BuildServiceProvider();
            var context = new DefaultHttpContext();

            context.Connection.RemoteIpAddress = IPAddress.Loopback;
            context.Request.Path = string.Concat(Path.AltDirectorySeparatorChar, Secret);
            context.Request.Headers.Authorization = Secret;
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
                new EndpointMetadataCollection(), EndpointName));

            await ServiceAudit.WriteAsync(context, services, NullLogger.Instance);

            var content = File.ReadAllText(Path.Combine(directory, AccessFile));

            Assert.Contains(EndpointName, content);
            Assert.DoesNotContain(Secret, content);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void DiagnosticsExcludeExceptionMessagesAndFrameworkPayloads()
    {
        var directory = TempDirectory();

        try
        {
            var options = new ServiceLogOptions { Directory = directory };
            using var files = new ServiceLogFiles(options);
            using var provider = new FileLoggerProvider(files, options);
            var framework = provider.CreateLogger(FrameworkCategory);
            var application = provider.CreateLogger(ApplicationCategory);

            framework.LogError(new InvalidOperationException(Secret), Secret);
            application.LogError(new InvalidOperationException(Secret), SafeMessage);

            var content = File.ReadAllText(Path.Combine(directory, DiagnosticFile));

            Assert.DoesNotContain(Secret, content);
            Assert.Contains(SafeMessage, content);
            Assert.Contains(nameof(InvalidOperationException), content);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void FileRotationKeepsOnlyConfiguredBackups()
    {
        var directory = TempDirectory();

        try
        {
            using var files = new ServiceLogFiles(new ServiceLogOptions
            {
                Directory = directory,
                MaximumFileBytes = MaximumFileBytes,
                RetainedFiles = RetainedFiles
            });

            for (var index = default(int); index < RecordCount; index++)
            {
                files.WriteAccess(new ServiceAccessRecord(DateTimeOffset.UtcNow,
                    EndpointName, IPAddress.Loopback.ToString(), HttpProtocol,
                    StatusCodes.Status204NoContent));
            }

            var paths = Directory.GetFiles(directory, RotationPattern);

            Assert.Equal(TotalFiles, paths.Length);

            foreach (var path in paths)
            {
                Assert.True(new FileInfo(path).Length <= MaximumFileBytes);

                foreach (var line in File.ReadLines(path))
                {
                    using var document = JsonDocument.Parse(line);

                    Assert.Equal(EndpointName,
                        document.RootElement.GetProperty(
                            nameof(ServiceAccessRecord.Endpoint)).GetString());
                }
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string TempDirectory() =>
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
}
