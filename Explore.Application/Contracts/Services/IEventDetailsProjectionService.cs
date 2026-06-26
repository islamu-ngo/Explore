// ABOUTME: Shared application service for building event detail DTOs from event aggregates.
// ABOUTME: Keeps public and management detail queries aligned without exposing persistence details.

using Explore.Application.DTOs.Event;

namespace Explore.Application.Contracts.Services;

public interface IEventDetailsProjectionService
{
    Task<EventDto?> BuildAsync(Guid eventId, CancellationToken cancellationToken);

    Task ResolveImageUrlsAsync(EventDto eventDto, CancellationToken cancellationToken);
}
