// ABOUTME: MediatR query request for fetching the full list of category types.
// ABOUTME: Returns IEnumerable<CategoryTypeDto>.
using Explore.Application.DTOs.CategoryType;
using MediatR;

namespace Explore.Application.Features.CategoryTypes.Requests.Queries;

public sealed record GetCategoryTypeListRequest : IRequest<List<CategoryTypeListDto>>
{
}
