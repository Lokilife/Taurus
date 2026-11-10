namespace Taurus.Core.Authentication.Keys;

public interface IAuthenticationKeys
{
    /// <summary>Authenticates the browser context using a versioned server key.</summary>
    string CsrfSeed(string tenantId, string origin, Guid userId, string sessionId);
}
