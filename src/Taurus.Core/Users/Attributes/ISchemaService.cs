namespace Taurus.Core.Users.Attributes;

/// <summary>
/// Checks and applies attribute schemas with explicit destructive approval.
/// </summary>
public interface ISchemaService
{
    Task CheckAsync(CancellationToken cancellationToken);

    Task<string> PlanAsync(CancellationToken cancellationToken);

    Task ApproveAsync(string fingerprint, CancellationToken cancellationToken);
}
