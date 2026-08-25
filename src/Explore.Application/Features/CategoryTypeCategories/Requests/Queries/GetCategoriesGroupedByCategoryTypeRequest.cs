// ABOUTME: MediatR query for fetching categories grouped by category type.
// ABOUTME: Returns grouped category structure for UI display.
using Explore.Application.DTOs.CategoryType;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public sealed record GetCategoriesGroupedByCategoryTypeRequest : IRequest<List<CategoryTypeWithCategoriesDto>>
{
}
