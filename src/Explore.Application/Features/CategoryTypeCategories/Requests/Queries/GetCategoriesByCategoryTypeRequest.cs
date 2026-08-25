// ABOUTME: MediatR query for fetching all categories in a specific category type.
// ABOUTME: Returns IEnumerable<CategoryDto>.
using Explore.Application.DTOs.Category;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public sealed record GetCategoriesByCategoryTypeRequest(int CategoryTypeId = default) : IRequest<List<CategoryListDto>>;
