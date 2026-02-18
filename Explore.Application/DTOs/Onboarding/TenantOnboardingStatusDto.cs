// ABOUTME: DTO describing tenant onboarding completion and user eligibility for tenant onboarding actions.
// ABOUTME: Used by startup flow to decide whether a tenant-admin policy questionnaire is required.

namespace Explore.Application.DTOs.Onboarding;

public class TenantOnboardingStatusDto
{
    public bool IsCompleted { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsCurrentUserTenantAdministrator { get; set; }
    public bool IsCurrentUserPlatformAdministrator { get; set; }
    public Guid TenantId { get; set; }
}
