// ABOUTME: MediatR query request for all EventSessionStatus lookup rows.
// ABOUTME: Returns a list of EventSessionStatusListDto for lookup enumeration endpoints.
using Explore.Application.DTOs.EventSessionStatus;
using MediatR;

namespace Explore.Application.Features.EventSessionStatuses.Requests.Queries;

public sealed record GetEventSessionStatusListRequest : IRequest<List<EventSessionStatusListDto>>
{
}
