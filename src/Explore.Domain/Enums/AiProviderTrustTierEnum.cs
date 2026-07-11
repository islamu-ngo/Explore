// ABOUTME: Evidence-based trust tier for the resolved AI provider endpoint (CTO correction #6).
// ABOUTME: Unknown (4) is the most restrictive and wins whenever evidence is ambiguous.

namespace Explore.Domain.Enums;

public enum AiProviderTrustTierEnum
{
    LocalInProcessOrSameNetworkModel = 0,
    TenantControlledPrivateEndpoint = 1,
    TenantConfiguredExternalProcessor = 2,
    PlatformConfiguredExternalProcessor = 3,
    Unknown = 4
}
