using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Commands;

public class DeleteCategoryTypeCategoriesCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
