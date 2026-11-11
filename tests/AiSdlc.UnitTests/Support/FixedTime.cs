namespace AiSdlc.UnitTests.Support;

/// <summary>A clock that always says the same moment.</summary>
public sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
