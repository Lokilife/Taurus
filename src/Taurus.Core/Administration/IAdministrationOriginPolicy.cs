namespace Taurus.Core.Administration;

public interface IAdministrationOriginPolicy
{
    /// <summary>Checks the tenant's configured admin origin.</summary>
    bool Allows(string tenantId, string origin);
}
