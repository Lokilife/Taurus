using Grpc.Core;
using Taurus.Shared;

namespace Taurus.Server.Transport.Grpc.Administration;

[BindServiceMethod(typeof(AdministrationServiceBinder),
    nameof(AdministrationServiceBinder.BindService))]
public sealed class AdministrationRpc(AdministrationAuthorization authorization)
    : Taurus.Shared.Administration.AdministrationBase
{
    /// <summary>Verifies access to the tenant administration surface.</summary>
    public override async Task<EmptyResponse> CheckAccess(
        EmptyRequest request,
        ServerCallContext context)
    {
        await authorization.RequireAsync(context.GetHttpContext(),
            context.CancellationToken);

        return new EmptyResponse();
    }
}
