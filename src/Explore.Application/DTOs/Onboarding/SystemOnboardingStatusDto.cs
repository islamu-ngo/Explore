// ABOUTME: Public, non-sensitive system startup state for BFF and onboarding clients.
// ABOUTME: Exposes only whether onboarding is required and which deployment mode should be shown.

namespace Explore.Application.DTOs.Onboarding;

public sealed class SystemOnboardingStatusDto
{
    public bool RequiresOnboarding { get; set; }
    public string DeploymentMode { get; set; } = "SingleTenant";
}
