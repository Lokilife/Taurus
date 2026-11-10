namespace Taurus.Core.Credentials.Opaque;

public interface IOpaqueService
{
    /// <summary>Enrolls only inside trusted local provisioning.</summary>
    string Enroll(string identifier, string password);

    /// <summary>Starts a bounded exchange; null records use a synthetic user.</summary>
    OpaqueChallenge Begin(
        string tenantId,
        string origin,
        string username,
        string? record,
        string request);

    /// <summary>Consumes one exchange and verifies its proof and scope.</summary>
    bool Finish(
        string tenantId,
        string origin,
        string username,
        string? record,
        string challengeId,
        string request);
}
