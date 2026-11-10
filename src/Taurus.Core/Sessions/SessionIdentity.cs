namespace Taurus.Core.Sessions;

public sealed record SessionIdentity(
    Guid UserId,
    Guid SessionId,
    string Username,
    string Locale,
    string Timezone);
