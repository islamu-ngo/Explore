// ABOUTME: Request contract for an explicit authorization policy package synchronization.
// ABOUTME: Carries optional one-time Cerbos Admin API credentials that are never persisted.

namespace Explore.Application.DTOs.Onboarding;

public sealed record AuthorizationPolicyPackageSyncRequestDto
{
    public string? AdminUsername { get; init; }

    public string? AdminPassword { get; init; }
}
