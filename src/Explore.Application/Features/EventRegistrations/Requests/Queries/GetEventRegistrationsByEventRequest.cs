// ABOUTME: MediatR query request for fetching registrations scoped to one event.
// ABOUTME: Returns a paginated EventRegistrationListDto page for organizer registration management.

using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Queries;

public class GetEventRegistrationsByEventRequest : IRequest<PaginatedResult<EventRegistrationListDto>>
{
    public Guid EventId { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
