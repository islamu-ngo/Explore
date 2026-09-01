// ABOUTME: Owns the Application-specific claim spelling used for resolved local platform users.
// ABOUTME: Keeps the internal user identifier separate from standard JWT and purpose-bound claims.

namespace Explore.Application.Authentication;

public static class PlatformIdentityClaimTypes
{
    public const string InternalUserId = "internal_user_id";
}

