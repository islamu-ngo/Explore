// ABOUTME: Captures exact owned and desired recurring Quartz identities for one host composition.
// ABOUTME: Lets startup remove disabled or retired platform keys without touching foreign scheduler rows.

using Quartz;

namespace Explore.API.Scheduling;

public sealed record QuartzRecurringJobManifest(
    IReadOnlySet<JobKey> Owned,
    IReadOnlySet<JobKey> Desired);
