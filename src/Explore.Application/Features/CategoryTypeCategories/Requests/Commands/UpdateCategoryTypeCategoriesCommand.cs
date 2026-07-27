// ABOUTME: MediatR command for updating a category-to-category-type link.
// ABOUTME: Carries server-owned junction identity and grouped relationship changes.
using Explore.Application.DTOs.CategoryTypeCategories;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Commands;

public class UpdateCategoryTypeCategoriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid CategoryTypeCategoriesId { get; set; }
    public required UpdateCategoryTypeCategoriesDto CategoryTypeCategoriesDto { get; set; }
}
