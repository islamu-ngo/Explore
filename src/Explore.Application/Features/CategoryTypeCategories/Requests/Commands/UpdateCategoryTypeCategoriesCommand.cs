// ABOUTME: MediatR command for updating a category-to-category-type link.
// ABOUTME: Carries the UpdateCategoryTypeCategoriesDto payload.
using Explore.Application.DTOs.CategoryTypeCategories;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Commands;

public class UpdateCategoryTypeCategoriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateCategoryTypeCategoriesDto CategoryTypeCategoriesDto { get; set; }
}
