// ABOUTME: MediatR query request for a single EventSessionStatus lookup row by ID.
// ABOUTME: Returns an EventSessionStatusDto for the detail endpoint.
using Explore.Application.DTOs.EventSessionStatus;
using MediatR;

namespace Explore.Application.Features.EventSessionStatuses.Requests.Queries;

public sealed record GetEventSessionStatusDetailsRequest : IRequest<EventSessionStatusDto>
{
    public int Id { get; init; }
}
