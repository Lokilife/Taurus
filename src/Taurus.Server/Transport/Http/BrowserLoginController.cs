using Microsoft.AspNetCore.Mvc;
using Taurus.Infrastructure.Configuration;

namespace Taurus.Server.Transport.Http;

/// <summary>Directs visitors to the prefixed static login application.</summary>
public sealed class BrowserLoginController(Settings settings) : ControllerBase
{
    /// <summary>Redirects the site root to the configured login UI.</summary>
    [HttpGet("/")]
    public IActionResult Login()
    {
        return Redirect(settings.Http.RequestPrefix + "/ui/");
    }
}
