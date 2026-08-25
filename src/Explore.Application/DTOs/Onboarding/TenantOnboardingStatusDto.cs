// ABOUTME: DTO describing tenant onboarding completion and user eligibility for tenant onboarding actions.
// ABOUTME: Used by startup flow to decide whether a tenant-admin policy questionnaire is required.

namespace Explore.Application.DTOs.Onboarding;

public sealed record TenantOnboardingStatusDto
{
    public bool IsCompleted { get; init; }
    public bool IsAuthenticated { get; init; }
    public bool IsCurrentUserTenantAdministrator { get; set; }
    public bool IsCurrentUserPlatformAdministrator { get; set; }
    public Guid TenantId { get; init; }
    public int CurrentStep { get; init; }
    public int TotalSteps { get; init; }
    public string[] CompletedSteps { get; init; } = Array.Empty<string>();
    public int ProgressPercentage { get; init; }
}
