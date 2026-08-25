// ABOUTME: MediatR query request for fetching all tag types.
// ABOUTME: Returns IEnumerable<TagTypeDto>.
using Explore.Application.DTOs.TagType;
using MediatR;

namespace Explore.Application.Features.TagTypes.Requests.Queries;

public sealed record GetTagTypeListRequest : IRequest<List<TagTypeListDto>>
{
}
