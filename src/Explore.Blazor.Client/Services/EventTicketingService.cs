// ABOUTME: Delegates event ticket catalog reads and mutations to the generated Event API client.
// ABOUTME: Preserves generated HAL resources, write DTOs, identifiers, cancellation, and API failures.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Pages.Studio;

namespace Explore.Blazor.Client.Services;

public sealed class EventTicketingService(IEventApiClient apiClient) : IEventTicketingService
{
    public async Task<EventTicketCatalogState?> GetCatalogAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        HalResourceOfEventTicketCatalogManagementDto resource =
            await apiClient.GetEventTicketCatalogManagementAsync(eventId, cancellationToken: cancellationToken);

        return EventTicketCatalogState.TryParse(resource, out EventTicketCatalogState? state)
            && state?.EventId == eventId
                ? state
                : null;
    }

    public Task<BaseCommandResponseOfGuid> CreateDraftAsync(
        Guid eventId,
        CreateEventTicketCatalogDraftCommand request,
        CancellationToken cancellationToken = default) =>
        apiClient.CreateEventTicketCatalogDraftAsync(eventId, request, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> CloneDraftAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        apiClient.CloneEventTicketCatalogDraftAsync(eventId, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> CreateTicketTypeAsync(
        Guid eventId,
        ManageEventTicketTypeDto request,
        CancellationToken cancellationToken = default) =>
        apiClient.CreateEventTicketTypeAsync(eventId, request, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> UpdateTicketTypeAsync(
        Guid eventId,
        Guid ticketTypeId,
        ManageEventTicketTypeDto request,
        CancellationToken cancellationToken = default) =>
        apiClient.UpdateEventTicketTypeAsync(eventId, ticketTypeId, request, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> DeleteTicketTypeAsync(
        Guid eventId,
        Guid ticketTypeId,
        CancellationToken cancellationToken = default) =>
        apiClient.DeleteEventTicketTypeAsync(eventId, ticketTypeId, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> CreateCapacityPoolAsync(
        Guid eventId,
        ManageEventCapacityPoolDto request,
        CancellationToken cancellationToken = default) =>
        apiClient.CreateEventCapacityPoolAsync(eventId, request, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> UpdateCapacityPoolAsync(
        Guid eventId,
        Guid capacityPoolId,
        ManageEventCapacityPoolDto request,
        CancellationToken cancellationToken = default) =>
        apiClient.UpdateEventCapacityPoolAsync(eventId, capacityPoolId, request, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> DeleteCapacityPoolAsync(
        Guid eventId,
        Guid capacityPoolId,
        CancellationToken cancellationToken = default) =>
        apiClient.DeleteEventCapacityPoolAsync(eventId, capacityPoolId, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> PublishAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        apiClient.PublishEventTicketCatalogAsync(eventId, cancellationToken: cancellationToken);
}
