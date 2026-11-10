namespace Taurus.Tests;

public sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset current = now;

    public override DateTimeOffset GetUtcNow() => current;

    public void Advance(TimeSpan elapsed) => current += elapsed;
}
