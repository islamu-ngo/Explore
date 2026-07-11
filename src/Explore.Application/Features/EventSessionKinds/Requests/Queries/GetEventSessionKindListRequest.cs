// ABOUTME: MediatR query request for fetching all event session kinds.
// ABOUTME: Returns list of EventSessionKindListDto values for program item classification.

using Explore.Application.DTOs.EventSessionKind;
using MediatR;

namespace Explore.Application.Features.EventSessionKinds.Requests.Queries;

public class GetEventSessionKindListRequest : IRequest<List<EventSessionKindListDto>>
{
}
