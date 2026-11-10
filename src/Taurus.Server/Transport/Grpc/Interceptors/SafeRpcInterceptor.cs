using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Taurus.Server.Transport.Grpc.Interceptors;

public sealed class SafeRpcInterceptor(ILogger<SafeRpcInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "Request could not be accepted"));
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled,
                "Request cancelled"));
        }
        catch
        {
            logger.LogError("RPC operation failed");

            throw new RpcException(new Status(StatusCode.Unavailable,
                "Operation could not be completed"));
        }
    }
}
