// ABOUTME: MediatR command for updating a category-to-category-type link.
// ABOUTME: Carries server-owned junction identity and grouped relationship changes.
using Explore.Application.DTOs.CategoryTypeCategories;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Commands;

public sealed record UpdateCategoryTypeCategoriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid CategoryTypeCategoriesId { get; init; }
    public required UpdateCategoryTypeCategoriesDto CategoryTypeCategoriesDto { get; init; }
}
