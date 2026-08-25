// ABOUTME: MediatR query request for fetching all category types containing a given category.
// ABOUTME: Returns IEnumerable<CategoryTypeDto>.
using Explore.Application.DTOs.CategoryType;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public sealed record GetCategoryTypesForCategoryRequest(Guid CategoryId = default) : IRequest<List<CategoryTypeListDto>>;
