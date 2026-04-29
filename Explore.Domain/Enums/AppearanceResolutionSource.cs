// ABOUTME: Enum for the resolved source of a user's appearance, tracked for provenance and display.
// ABOUTME: Replaces magic strings with an explicit contract so the frontend does not need fragile string matching.

namespace Explore.Domain.Enums;

public enum AppearanceResolutionSource
{
    UserTenantProfile,
    UserGlobalProfile,
    TenantDefaultPreset,
    InstanceDefaultPreset,
    SystemPresetFallback,
    EmergencyFallback
}