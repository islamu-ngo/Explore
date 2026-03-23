// ABOUTME: MediatR query request for fetching a paginated tag-type/tag link list.
// ABOUTME: Returns IEnumerable<TagTypeTagsListDto>.
using Explore.Application.DTOs.TagTypeTags;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Requests.Queries;

public class GetTagTypeTagsListRequest : IRequest<List<TagTypeTagsListDto>>
{
}
