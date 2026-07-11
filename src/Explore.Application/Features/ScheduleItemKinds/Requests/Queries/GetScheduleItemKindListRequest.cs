// ABOUTME: MediatR query request for fetching all schedule item kinds.
// ABOUTME: Returns list of ScheduleItemKindListDto (Break, Ceremony, Keynote, etc.).

using Explore.Application.DTOs.ScheduleItemKind;
using MediatR;

namespace Explore.Application.Features.ScheduleItemKinds.Requests.Queries;

public class GetScheduleItemKindListRequest : IRequest<List<ScheduleItemKindListDto>>
{
}
