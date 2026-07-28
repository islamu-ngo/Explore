// ABOUTME: Grouped PATCH contract for instance reporting-provider delegation locks.
// ABOUTME: Allows general, Osprey, and Coop locks to change independently.

namespace Explore.Application.DTOs.EventReporting;

public sealed class UpdateReportingProviderLocksDto
{
    public ReportingProviderLockUpdateDto? General { get; set; }
    public ReportingProviderLockUpdateDto? Osprey { get; set; }
    public ReportingProviderLockUpdateDto? Coop { get; set; }
}

public sealed class ReportingProviderLockUpdateDto
{
    public bool Locked { get; set; }
}
