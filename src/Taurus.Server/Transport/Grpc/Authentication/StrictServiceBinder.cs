using Grpc.Core;
using Taurus.Server.Transport.Grpc.Serialization;
using Taurus.Shared;

namespace Taurus.Server.Transport.Grpc.Authentication;

public static class StrictServiceBinder
{
    private const string ServiceName = "taurus.v1.Authentication";

    public static void BindService(
        ServiceBinderBase binder,
        Taurus.Shared.Authentication.AuthenticationBase? service)
    {
        binder.AddMethod(Method<BeginLoginRequest, BeginLoginResponse>(
            nameof(AuthenticationRpc.BeginLogin)),
            service is null ? null : service.BeginLogin);
        binder.AddMethod(Method<LoginRequest, SessionResponse>(
            nameof(AuthenticationRpc.Login)),
            service is null ? null : service.Login);
        binder.AddMethod(Method<EmptyRequest, SessionResponse>(
            nameof(AuthenticationRpc.CurrentSession)),
            service is null ? null : service.CurrentSession);
        binder.AddMethod(Method<EmptyRequest, EmptyResponse>(
            nameof(AuthenticationRpc.Logout)),
            service is null ? null : service.Logout);
    }

    private static Method<TRequest, TResponse> Method<TRequest, TResponse>(string name)
        where TRequest : class, Google.Protobuf.IMessage<TRequest>, new()
        where TResponse : class, Google.Protobuf.IMessage<TResponse>, new() => new(
            MethodType.Unary,
            ServiceName,
            name,
            StrictProtobufMarshaller.Create<TRequest>(),
            StrictProtobufMarshaller.Create<TResponse>());
}
