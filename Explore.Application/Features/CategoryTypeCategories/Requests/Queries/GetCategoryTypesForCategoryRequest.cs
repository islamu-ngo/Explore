using Explore.Application.DTOs.CategoryType;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public class GetCategoryTypesForCategoryRequest : IRequest<List<CategoryTypeListDto>>
{
    public Guid CategoryId { get; set; }
}
