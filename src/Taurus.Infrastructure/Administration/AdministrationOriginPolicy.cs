using Taurus.Core.Administration;
using Taurus.Infrastructure.Configuration;

namespace Taurus.Infrastructure.Administration;

public sealed class AdministrationOriginPolicy(Settings settings)
    : IAdministrationOriginPolicy
{
    public bool Allows(string tenantId, string origin)
    {
        var tenant = settings.Resolve(origin);

        return tenant?.Id == tenantId && tenant.AdminOrigin is not null
            && string.Equals(tenant.AdminOrigin, origin,
                StringComparison.OrdinalIgnoreCase);
    }
}
