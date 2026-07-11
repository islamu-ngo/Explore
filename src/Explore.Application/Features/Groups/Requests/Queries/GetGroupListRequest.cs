// ABOUTME: MediatR query request for retrieving a paginated list of all Groups.
// ABOUTME: Returns PaginatedResult<GroupListDto> for admin/listing purposes.

using Explore.Application.DTOs.Group;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Groups.Requests.Queries;

public class GetGroupListRequest : IRequest<PaginatedResult<GroupListDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
