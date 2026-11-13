using MediatR;
using Microsoft.AspNetCore.Mvc;
using Taurus.Application.Features.Sessions.CurrentSession;
using Taurus.Infrastructure.Configuration;
using Taurus.Server.Security;

namespace Taurus.Server.Transport.Http;

/// <summary>Validates the current session for reverse-proxy authorization.</summary>
[Route("[taurus]/auth")]
public sealed class ForwardAuthController(Settings settings, ISender sender)
    : ControllerBase
{
    /// <summary>Returns identity headers or an unauthorized, bodyless response.</summary>
    [HttpGet]
    public async Task<IActionResult> Authorize()
    {
        var origin = BrowserSecurity.Origin(HttpContext);
        var tenant = settings.Resolve(origin)!;
        var identity = await sender.Send(new CurrentSessionQuery(
            tenant.Id, origin, BrowserSecurity.ReadSession(HttpContext)),
            HttpContext.RequestAborted);

        if (identity is null)
            return StatusCode(StatusCodes.Status401Unauthorized);

        Response.Headers[BrowserSecurity.UserIdHeader] = identity.UserId.ToString();
        Response.Headers[BrowserSecurity.UsernameHeader] =
            Uri.EscapeDataString(identity.Username);
        Response.Headers[BrowserSecurity.TenantHeader] = Uri.EscapeDataString(tenant.Id);

        return NoContent();
    }
}
