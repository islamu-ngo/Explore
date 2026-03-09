// ABOUTME: Enumerates the privacy posture for analytics identity handling.
// ABOUTME: Used to gate raw identifiers and identify/group calls across providers.

namespace Explore.Application.Analytics;

public enum AnalyticsConsentMode
{
    Anonymous = 0,
    Pseudonymous = 1,
    Identified = 2
}
