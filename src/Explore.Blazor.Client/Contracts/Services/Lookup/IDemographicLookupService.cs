// ABOUTME: Contract for demographic audience reference lookups (gender options, age brackets).
// ABOUTME: Encapsulates audience filtering and targeting taxonomies for event forms and discovery.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface IDemographicLookupService
{
    Task<ICollection<AudienceGenderListDto>> GetAudienceGendersAsync();
    Task<ICollection<AudienceAgeListDto>> GetAudienceAgesAsync();
}
