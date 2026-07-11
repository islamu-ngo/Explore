// ABOUTME: MediatR query request for fetching a single category-type/category link by ID.
// ABOUTME: Returns CategoryTypeCategoriesDto.
using Explore.Application.DTOs.CategoryTypeCategories;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public class GetCategoryTypeCategoriesDetailsRequest : IRequest<CategoryTypeCategoriesDto>
{
    public Guid Id { get; set; }
}
