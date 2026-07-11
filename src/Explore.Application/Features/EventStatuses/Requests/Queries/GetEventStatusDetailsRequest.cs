// ABOUTME: MediatR query request for fetching a single event status by ID.
// ABOUTME: Returns EventStatusDto.
using Explore.Application.DTOs.EventStatus;
using MediatR;

namespace Explore.Application.Features.EventStatuses.Requests.Queries;

public class GetEventStatusDetailsRequest : IRequest<EventStatusDto>
{
    public int Id { get; set; }
}
