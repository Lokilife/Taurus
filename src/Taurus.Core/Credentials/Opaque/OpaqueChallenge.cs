namespace Taurus.Core.Credentials.Opaque;

public sealed record OpaqueChallenge(string Id, string Response)
{
    public override string ToString() => nameof(OpaqueChallenge);
}
