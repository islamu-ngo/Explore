// ABOUTME: Runtime settings for expired idempotency replay-cache cleanup.
// ABOUTME: Controls scheduling, grace period, batch size, and dry-run safety mode.

namespace Explore.Infrastructure;

public sealed class IdempotencyCleanupSettings
{
    public const string SectionName = "IdempotencyCleanup";

    public bool Enabled { get; set; } = true;
    public bool DryRun { get; set; }
    public int InitialDelaySeconds { get; set; } = 30;
    public int PollingIntervalMinutes { get; set; } = 60;
    public int BatchSize { get; set; } = 500;
    public int ExpirationGraceHours { get; set; } = 24;
}
