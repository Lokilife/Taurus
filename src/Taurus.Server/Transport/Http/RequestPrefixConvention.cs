using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Taurus.Server.Transport.Http;

/// <summary>Resolves the Taurus route token from the configured request prefix.</summary>
public sealed class RequestPrefixConvention(string prefix) : IApplicationModelConvention
{
    private const string PrefixToken = "taurus";
    private const char PathSeparator = '/';

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
            controller.RouteValues[PrefixToken] = prefix.Trim(PathSeparator);
    }
}
