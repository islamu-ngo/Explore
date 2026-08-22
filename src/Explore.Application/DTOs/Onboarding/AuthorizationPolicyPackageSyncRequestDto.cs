// ABOUTME: Request contract for an explicit authorization policy package synchronization.
// ABOUTME: Carries optional one-time Cerbos Admin API credentials that are never persisted.

namespace Explore.Application.DTOs.Onboarding;

public sealed class AuthorizationPolicyPackageSyncRequestDto
{
    public string? AdminUsername { get; set; }

    public string? AdminPassword { get; set; }
}
