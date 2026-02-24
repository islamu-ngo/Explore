using Explore.Application.DTOs.CategoryType;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public class GetCategoriesGroupedByCategoryTypeRequest : IRequest<List<CategoryTypeWithCategoriesDto>>
{
}
