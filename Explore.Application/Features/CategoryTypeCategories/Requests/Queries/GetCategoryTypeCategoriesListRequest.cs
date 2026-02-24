using Explore.Application.DTOs.CategoryTypeCategories;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public class GetCategoryTypeCategoriesListRequest : IRequest<List<CategoryTypeCategoriesListDto>>
{
}
