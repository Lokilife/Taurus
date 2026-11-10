namespace Taurus.Infrastructure.Authentication.Opaque;

internal sealed record PendingLogin(
    string TenantId,
    string Origin,
    string Username,
    string? Record,
    string State,
    DateTimeOffset ExpiresAt)
{
    public override string ToString() => nameof(PendingLogin);
}
