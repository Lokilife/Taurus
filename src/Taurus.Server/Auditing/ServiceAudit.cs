using Grpc.AspNetCore.Server;
using Taurus.Infrastructure.Logging;

namespace Taurus.Server.Auditing;

public static class ServiceAudit
{
    private const string AccessEvent = "endpoint-access";
    private const string HttpProtocol = "HTTP";
    private const string RpcProtocol = "gRPC";

    public static Task WriteAsync(
        HttpContext context,
        IServiceProvider services,
        ILogger logger)
    {
        try
        {
            var files = services.GetRequiredService<ServiceLogFiles>();
            var rpc = context.Features.Get<IServerCallContextFeature>();

            files.WriteAccess(new ServiceAccessRecord(
                DateTimeOffset.UtcNow,
                context.GetEndpoint()?.DisplayName ?? AccessEvent,
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                rpc is null ? HttpProtocol : RpcProtocol,
                rpc is null ? context.Response.StatusCode
                    : (int)rpc.ServerCallContext.Status.StatusCode));
        }
        catch (IOException)
        {
            logger.LogError("Service access log unavailable");
        }
        catch (UnauthorizedAccessException)
        {
            logger.LogError("Service access log unavailable");
        }

        return Task.CompletedTask;
    }
}
