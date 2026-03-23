// ABOUTME: MediatR query request for fetching all category types containing a given category.
// ABOUTME: Returns IEnumerable<CategoryTypeDto>.
using Explore.Application.DTOs.CategoryType;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public class GetCategoryTypesForCategoryRequest : IRequest<List<CategoryTypeListDto>>
{
    public Guid CategoryId { get; set; }
}
