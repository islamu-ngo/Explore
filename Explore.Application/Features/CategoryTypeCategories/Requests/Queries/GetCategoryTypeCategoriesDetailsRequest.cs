using Explore.Application.DTOs.CategoryTypeCategories;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Requests.Queries;

public class GetCategoryTypeCategoriesDetailsRequest : IRequest<CategoryTypeCategoriesDto>
{
    public Guid Id { get; set; }
}
