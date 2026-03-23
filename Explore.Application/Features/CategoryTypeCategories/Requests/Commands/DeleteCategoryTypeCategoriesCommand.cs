// ABOUTME: MediatR command for deleting a category-to-category-type link by ID.
// ABOUTME: Carries the target junction record ID.
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Commands;

public class DeleteCategoryTypeCategoriesCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
