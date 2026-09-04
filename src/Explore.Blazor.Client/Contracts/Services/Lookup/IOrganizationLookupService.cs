// ABOUTME: Contract for organization and actor reference lookups (positions, actor types, approval statuses).
// ABOUTME: Encapsulates entity role and verification taxonomies for administration and member management.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface IOrganizationLookupService
{
    Task<ICollection<OrganizationPositionListDto>> GetOrganizationPositionsAsync();
    Task<ICollection<ActorTypeListDto>> GetActorTypesAsync();
    Task<ICollection<StatusTypeListDto>> GetApprovalStatusesAsync();
}
