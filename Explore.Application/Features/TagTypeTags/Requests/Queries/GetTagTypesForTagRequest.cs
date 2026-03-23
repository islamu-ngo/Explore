// ABOUTME: MediatR query for fetching all tag types containing a given tag.
// ABOUTME: Returns IEnumerable<TagTypeDto>.
using Explore.Application.DTOs.TagType;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Requests.Queries;

public class GetTagTypesForTagRequest : IRequest<List<TagTypeListDto>>
{
    public Guid TagId { get; set; }
}
