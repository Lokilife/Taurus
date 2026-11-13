namespace Taurus.Core.Administration;

public interface IAdministrationRepo
{
    /// <summary>Checks the persisted root marker and current enabled status.</summary>
    Task<bool> IsRootAsync(
        string tenantId,
        Guid userId,
        CancellationToken cancellationToken);
}
