// ABOUTME: Catalogs non-grantable scopes reserved for trusted in-process machine principals.
// ABOUTME: Keeps background-worker authority narrower than tenant or instance administrator API-key scopes.

namespace Explore.Application.Authorization;

public static class InternalMachineScopes
{
    public const string ProcessIncomingWebhook = "internal:webhook:process-incoming";
}
