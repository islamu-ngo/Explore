// ABOUTME: Dedicated HTTP request for atomically completing tenant onboarding.
// ABOUTME: Keeps mandatory legal identity and its revision separate from general policy updates.

using Explore.Application.DTOs.TenantPolicy;
using Explore.Application.DTOs.TenantSettings;

namespace Explore.Application.DTOs.Onboarding;

public sealed record CompleteTenantOnboardingRequest
{
    public required UpdateTenantPolicyRequest Settings { get; init; } = new();
    public required TenantDirectoryOperatorIdentityInputDto DirectoryOperatorIdentity { get; init; }
    public Guid? ExpectedDirectoryOperatorIdentityConcurrencyStamp { get; init; }
}
