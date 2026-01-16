using Explore.Application.DTOs.EventCategories;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.EventCategories.Requests.Queries
{
    public class GetEventCategoriesListRequest : IRequest<List<EventCategoriesListDto>>
    {
    }
}
