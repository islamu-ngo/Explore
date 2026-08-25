// ABOUTME: MediatR query request for retrieving Groups the current user belongs to.
// ABOUTME: Returns PaginatedResult<GroupListDto> with the normalized CurrentUserRoleId populated per group.

using Explore.Application.DTOs.Group;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Groups.Requests.Queries;

public sealed record GetMyGroupsRequest : IRequest<PaginatedResult<GroupListDto>>
{
    public required string UserId { get; init; } = string.Empty;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
