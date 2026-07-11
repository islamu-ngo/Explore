// ABOUTME: MediatR query request for fetching a paginated AT Protocol record list.
// ABOUTME: Returns IEnumerable<AtprotoRecordListDto>.
using Explore.Application.DTOs.AtprotoRecord;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Requests.Queries;

public class GetAtprotoRecordListRequest : IRequest<List<AtprotoRecordListDto>>
{
}
