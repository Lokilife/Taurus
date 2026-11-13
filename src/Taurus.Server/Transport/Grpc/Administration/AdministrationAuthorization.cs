using Grpc.Core;
using MediatR;
using Taurus.Application.Features.Administration.Authorize;
using Taurus.Core.Sessions;
using Taurus.Infrastructure.Configuration;
using Taurus.Server.Security;

namespace Taurus.Server.Transport.Grpc.Administration;

/// <summary>Resolves administration authority from the HTTP request.</summary>
public sealed class AdministrationAuthorization(Settings settings, ISender sender)
{
    private const int PrimaryErrorIndex = 0;

    /// <summary>Requires a dedicated admin origin and an enabled root session.</summary>
    public async Task<SessionIdentity> RequireAsync(
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var origin = BrowserSecurity.Origin(http);
        var tenant = settings.Resolve(origin);

        if (tenant?.AdminOrigin is null || !string.Equals(
            tenant.AdminOrigin, origin, StringComparison.OrdinalIgnoreCase))
            throw new RpcException(new Status(StatusCode.PermissionDenied,
                "Administration access denied"));

        var result = await sender.Send(new AuthorizeAdministrationQuery(
            tenant.Id, origin, BrowserSecurity.ReadSession(http)), cancellationToken);

        if (result.IsFailed)
        {
            var unauthenticated = result.Errors[PrimaryErrorIndex]
                is AdministrationError error
                && error.Failure == AdministrationFailure.Unauthenticated;

            throw new RpcException(new Status(unauthenticated
                ? StatusCode.Unauthenticated : StatusCode.PermissionDenied,
                "Administration access denied"));
        }

        return result.Value;
    }
}
