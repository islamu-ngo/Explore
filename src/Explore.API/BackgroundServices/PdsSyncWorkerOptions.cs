// ABOUTME: Configures bounded polling, claim leases, batches, and concurrency for PDS outbox delivery.
// ABOUTME: Keeps operational throughput controls separate from tenant ATProto capability and consent settings.

namespace Explore.API.BackgroundServices;

public sealed class PdsSyncWorkerOptions
{
    public const string SectionName = "Atproto:PdsSync";

    public bool Enabled { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 20;
    public int MaxConcurrency { get; set; } = 10;
    public int LeaseDurationSeconds { get; set; } = 90;
}
