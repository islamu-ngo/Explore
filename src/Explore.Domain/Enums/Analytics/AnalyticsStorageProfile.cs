// ABOUTME: Computed runtime storage profile — describes effective browser storage behavior.
// ABOUTME: Drives consent banner visibility and analytics initialization timing.

namespace Explore.Domain.Enums.Analytics;

public enum AnalyticsStorageProfile
{
    Cookieless = 0,
    ConsentManaged = 1,
    FullConsent = 2
}
