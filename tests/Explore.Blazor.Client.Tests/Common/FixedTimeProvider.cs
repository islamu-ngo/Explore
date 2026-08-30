// ABOUTME: Provides an immutable clock for deterministic Blazor component and service tests.
// ABOUTME: Prevents current-date and year-boundary behavior from depending on the executing machine.

namespace Explore.Blazor.Client.Tests.Common;

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

internal static class TestTime
{
    internal static readonly DateTimeOffset UtcNow =
        new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
}
