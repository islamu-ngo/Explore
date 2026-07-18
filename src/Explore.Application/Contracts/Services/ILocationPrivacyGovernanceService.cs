// ABOUTME: Exposes the effective instance-and-tenant ceiling for EventLocation disclosure.
// ABOUTME: Returns only conservative values when stored governance cannot be resolved safely.

using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Services;

public enum LocationPrivacyGovernanceReasonCode
{
    Resolved = 1,
    InvalidTenantId = 2,
    InvalidInstanceSetting = 3,
    InvalidTenantSetting = 4,
    RepositoryUnavailable = 5
}

public sealed record EffectiveLocationPrivacyGovernance(
    bool IsResolved,
    LocationPrivacyGovernanceReasonCode ReasonCode,
    bool AllowHomeLocations,
    bool AllowPublicExactAddress,
    bool AllowPublicCoordinates,
    LocationDisclosureAudienceEnum MinimumHomeAudience,
    TimeSpan DefaultRevealOffset)
{
    public static EffectiveLocationPrivacyGovernance FailClosed(
        LocationPrivacyGovernanceReasonCode reasonCode)
        => new(
            IsResolved: false,
            reasonCode,
            AllowHomeLocations: false,
            AllowPublicExactAddress: false,
            AllowPublicCoordinates: false,
            MinimumHomeAudience: LocationDisclosureAudienceEnum.Never,
            DefaultRevealOffset: TimeSpan.FromDays(30));
}

public interface ILocationPrivacyGovernanceService
{
    Task<EffectiveLocationPrivacyGovernance> ResolveAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
