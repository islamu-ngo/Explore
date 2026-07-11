// ABOUTME: Preset theme origin for display provenance — distinguishes system, tenant, user-custom, and fallback sources.
// ABOUTME: Carried in ResolvedThemeDto so the UI knows whether a theme is editable, clonable, or read-only.

namespace Explore.Domain.Enums;

public enum AppearanceThemeOrigin
{
    SystemPreset,
    TenantPreset,
    UserCustom,
    Fallback
}
