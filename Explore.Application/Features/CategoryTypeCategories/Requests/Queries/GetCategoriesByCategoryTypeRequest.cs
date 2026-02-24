using Explore.Application.DTOs.Category;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public class GetCategoriesByCategoryTypeRequest : IRequest<List<CategoryListDto>>
{
    public int CategoryTypeId { get; set; }
}
