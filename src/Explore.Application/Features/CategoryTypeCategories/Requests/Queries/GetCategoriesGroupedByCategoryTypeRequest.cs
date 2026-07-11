// ABOUTME: MediatR query for fetching categories grouped by category type.
// ABOUTME: Returns grouped category structure for UI display.
using Explore.Application.DTOs.CategoryType;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public class GetCategoriesGroupedByCategoryTypeRequest : IRequest<List<CategoryTypeWithCategoriesDto>>
{
}
