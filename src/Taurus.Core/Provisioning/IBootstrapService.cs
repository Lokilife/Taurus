namespace Taurus.Core.Provisioning;

/// <summary>Creates missing tenant roots without resetting existing identities.</summary>
public interface IBootstrapService
{
    Task RunAsync(CancellationToken cancellationToken);
}
