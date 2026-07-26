// ABOUTME: Defines global moderation transitions for subjects and AT Protocol credentials.
// ABOUTME: Keeps suspend and reinstate evidence explicit without granting tenant-scoped authority.

namespace Explore.Domain.Enums;

public enum GlobalModerationAction
{
    Suspend = 1,
    Reinstate = 2
}
