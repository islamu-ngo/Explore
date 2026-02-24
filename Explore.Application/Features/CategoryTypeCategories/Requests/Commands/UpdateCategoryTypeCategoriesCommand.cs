using Explore.Application.DTOs.CategoryTypeCategories;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Commands;

public class UpdateCategoryTypeCategoriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateCategoryTypeCategoriesDto CategoryTypeCategoriesDto { get; set; }
}
