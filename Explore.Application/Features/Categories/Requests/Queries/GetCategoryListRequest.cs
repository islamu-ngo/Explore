using System.Collections.Generic;
using Explore.Application.DTOs.Category;
using MediatR;

namespace Explore.Application.Features.Categories.Requests.Queries
{
    public class GetCategoryListRequest : IRequest<List<CategoryListDto>>
    {
    }
}
