// ABOUTME: Grouped PATCH contract for instance reporting-provider delegation locks.
// ABOUTME: Allows general, Osprey, and Coop locks to change independently.

namespace Explore.Application.DTOs.EventReporting;

public sealed record UpdateReportingProviderLocksDto
{
    public ReportingProviderLockUpdateDto? General { get; init; }
    public ReportingProviderLockUpdateDto? Osprey { get; init; }
    public ReportingProviderLockUpdateDto? Coop { get; init; }
}

public sealed record ReportingProviderLockUpdateDto
{
    public bool Locked { get; init; }
}
