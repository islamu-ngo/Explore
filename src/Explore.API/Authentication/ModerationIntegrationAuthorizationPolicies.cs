// ABOUTME: Authorization policy names for moderation-provider integration callbacks.
// ABOUTME: Keeps integration endpoints scoped to authenticated machine callers.

namespace Explore.API.Authentication;

public static class ModerationIntegrationAuthorizationPolicies
{
    public const string OspreyCallback = "ModerationIntegration.OspreyCallback";
    public const string CoopCallback = "ModerationIntegration.CoopCallback";
}
