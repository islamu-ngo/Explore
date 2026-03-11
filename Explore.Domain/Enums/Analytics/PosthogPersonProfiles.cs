// ABOUTME: PostHog person_profiles configuration controlling user profile creation.
// ABOUTME: IdentifiedOnly = profiles only for identified users; Never = anonymous website analytics only.

namespace Explore.Domain.Enums.Analytics;

public enum PosthogPersonProfiles
{
    Always = 0,
    IdentifiedOnly = 1,
    Never = 2
}
