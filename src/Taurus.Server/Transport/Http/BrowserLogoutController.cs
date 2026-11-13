using MediatR;
using Microsoft.AspNetCore.Mvc;
using Taurus.Application.Features.Authentication.Logout;
using Taurus.Infrastructure.Configuration;
using Taurus.Server.Security;

namespace Taurus.Server.Transport.Http;

/// <summary>Revokes the visitor's current session through browser navigation.</summary>
[Route("[taurus]/logout")]
public sealed class BrowserLogoutController(Settings settings, ISender sender)
    : ControllerBase
{
    private const string FetchSite = "Sec-Fetch-Site";
    private const string FetchMode = "Sec-Fetch-Mode";
    private const string FetchDestination = "Sec-Fetch-Dest";
    private const string FetchUser = "Sec-Fetch-User";
    private const string SameOrigin = "same-origin";
    private const string DirectNavigation = "none";
    private const string Navigation = "navigate";
    private const string Document = "document";
    private const string UserActivated = "?1";

    /// <summary>Revokes the current session on a user-activated navigation.</summary>
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        var http = HttpContext;

        if (!HttpMethods.IsGet(http.Request.Method))
            return StatusCode(StatusCodes.Status405MethodNotAllowed);

        var headers = http.Request.Headers;
        var site = headers[FetchSite].ToString();

        if ((site != SameOrigin && site != DirectNavigation)
            || headers[FetchMode] != Navigation
            || headers[FetchDestination] != Document
            || headers[FetchUser] != UserActivated)
            return StatusCode(StatusCodes.Status403Forbidden);

        var origin = BrowserSecurity.Origin(http);
        var tenant = settings.Resolve(origin)!;
        var token = BrowserSecurity.ReadSession(http);

        if (token.Length != default)
        {
            var revoked = await sender.Send(new LogoutCommand(tenant.Id, origin, token),
                http.RequestAborted);

            if (!revoked)
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        http.Response.Cookies.Delete(BrowserSecurity.SessionCookieName(http),
            BrowserSecurity.Cookie(http));

        return Redirect(settings.Http.RequestPrefix + "/ui/");
    }
}
