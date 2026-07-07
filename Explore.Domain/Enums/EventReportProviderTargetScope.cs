// ABOUTME: Scope discriminator for event-report moderation provider targets.
// ABOUTME: Separates local, instance-level, and tenant-owned provider provenance.

namespace Explore.Domain.Enums;

public enum EventReportProviderTargetScope
{
    Local = 1,
    Instance = 2,
    Tenant = 3
}
