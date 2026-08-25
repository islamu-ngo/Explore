// ABOUTME: MediatR query for fetching all tag types containing a given tag.
// ABOUTME: Returns IEnumerable<TagTypeDto>.
using Explore.Application.DTOs.TagType;
using MediatR;

namespace Explore.Application.Features.TagTypeTags.Requests.Queries;

public sealed record GetTagTypesForTagRequest(Guid TagId = default) : IRequest<List<TagTypeListDto>>;
