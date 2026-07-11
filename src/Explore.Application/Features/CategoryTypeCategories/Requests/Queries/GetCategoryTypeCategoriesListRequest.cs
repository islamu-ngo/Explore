// ABOUTME: MediatR query request for fetching a paginated category-type/category link list.
// ABOUTME: Returns IEnumerable<CategoryTypeCategoriesListDto>.
using Explore.Application.DTOs.CategoryTypeCategories;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public class GetCategoryTypeCategoriesListRequest : IRequest<List<CategoryTypeCategoriesListDto>>
{
}
