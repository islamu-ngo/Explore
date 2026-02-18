// ABOUTME: Enum for supported analytics providers in the pluggable analytics system.
// ABOUTME: Used by runtime analytics resolution and lookup-table id mapping.

namespace Explore.Domain.Enums;

public enum AnalyticsProviderEnum
{
    None = 0,
    Posthog = 1,
    Plausible = 2,
    Rybbit = 3,
    RudderStack = 4
}
