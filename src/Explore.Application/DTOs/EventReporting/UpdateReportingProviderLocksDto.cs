// ABOUTME: Request DTO for instance-level moderation reporting provider delegation locks.
// ABOUTME: Controls whether tenants may configure global, Osprey, or Coop reporting overrides.

namespace Explore.Application.DTOs.EventReporting;

public sealed class UpdateReportingProviderLocksDto
{
    public bool LockReportingProviders { get; init; } = true;

    public bool LockTenantOspreyProvider { get; init; } = true;

    public bool LockTenantCoopProvider { get; init; } = true;
}
