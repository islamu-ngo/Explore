// ABOUTME: MediatR command for creating a category-to-category-type link.
// ABOUTME: Carries the CreateCategoryTypeCategoriesDto payload.
using Explore.Application.DTOs.CategoryTypeCategories;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Commands;

public sealed record CreateCategoryTypeCategoriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateCategoryTypeCategoriesDto CategoryTypeCategoriesDto { get; init; }
}
