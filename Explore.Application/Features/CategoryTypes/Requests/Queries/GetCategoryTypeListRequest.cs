using Explore.Application.DTOs.CategoryType;
using MediatR;

namespace Explore.Application.Features.CategoryTypes.Requests.Queries;

public class GetCategoryTypeListRequest : IRequest<List<CategoryTypeListDto>>
{
}
