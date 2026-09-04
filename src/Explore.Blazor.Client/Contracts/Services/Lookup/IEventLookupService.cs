// ABOUTME: Contract for event domain reference lookups (types, formats, statuses, session kinds, modes, visibility).
// ABOUTME: Encapsulates read-only event taxonomy queries for UI forms and catalog filters.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Lookup;

public interface IEventLookupService
{
    Task<ICollection<EventTypeListDto>> GetEventTypesAsync();
    Task<ICollection<EventFormatListDto>> GetEventFormatsAsync();
    Task<ICollection<EventStatusListDto>> GetEventStatusesAsync();
    Task<ICollection<EventSessionKindListDto>> GetEventSessionKindsAsync();
    Task<ICollection<RegistrationModeListDto>> GetRegistrationModesAsync();
    Task<ICollection<VisibilityTypeListDto>> GetVisibilityTypesAsync();
}
