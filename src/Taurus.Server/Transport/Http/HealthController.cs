using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Taurus.Infrastructure.Persistence;
using Taurus.Infrastructure.Schemas;

namespace Taurus.Server.Transport.Http;

/// <summary>Exposes unprefixed process and storage readiness probes.</summary>
[Route("health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Reports that the HTTP process is responding.</summary>
    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok();
    }

    /// <summary>Checks storage, migrations and schema readiness.</summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(
        [FromServices] IdentityDatabase db,
        [FromServices] SchemaManager schemas)
    {
        try
        {
            if (!await db.Database.CanConnectAsync(HttpContext.RequestAborted))
                return StatusCode(StatusCodes.Status503ServiceUnavailable);

            foreach (var migration in await db.Database.GetPendingMigrationsAsync(
                HttpContext.RequestAborted))
                return StatusCode(StatusCodes.Status503ServiceUnavailable);

            await schemas.CheckAsync(HttpContext.RequestAborted);

            return Ok();
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
