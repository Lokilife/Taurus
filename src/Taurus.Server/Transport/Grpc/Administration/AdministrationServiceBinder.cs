using Grpc.Core;
using Taurus.Server.Transport.Grpc.Serialization;
using Taurus.Shared;

namespace Taurus.Server.Transport.Grpc.Administration;

public static class AdministrationServiceBinder
{
    private const string ServiceName = "taurus.v1.Administration";

    public static void BindService(
        ServiceBinderBase binder,
        Taurus.Shared.Administration.AdministrationBase? service)
    {
        var method = new Method<EmptyRequest, EmptyResponse>(MethodType.Unary,
            ServiceName, nameof(AdministrationRpc.CheckAccess),
            StrictProtobufMarshaller.Create<EmptyRequest>(),
            StrictProtobufMarshaller.Create<EmptyResponse>());

        binder.AddMethod(method, service is null ? null : service.CheckAccess);
    }
}
