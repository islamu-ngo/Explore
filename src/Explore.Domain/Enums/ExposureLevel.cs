// ABOUTME: Governs how a custom property may be surfaced outside internal administration workflows.
// ABOUTME: Used by shared definitions, templates, runtime definitions, and projections.

namespace Explore.Domain.Enums;

public enum ExposureLevel
{
    Internal = 1,
    OrganizerOnly = 2,
    TenantAdminOnly = 3,
    Public = 4
}
