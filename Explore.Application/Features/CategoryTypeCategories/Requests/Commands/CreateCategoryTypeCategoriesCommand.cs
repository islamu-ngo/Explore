using Explore.Application.DTOs.CategoryTypeCategories;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Commands;

public class CreateCategoryTypeCategoriesCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateCategoryTypeCategoriesDto CategoryTypeCategoriesDto { get; set; }
}
