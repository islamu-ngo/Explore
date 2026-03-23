// ABOUTME: MediatR query request for fetching a paginated event-category link list.
// ABOUTME: Returns IEnumerable<EventCategoriesListDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.EventCategories;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Queries;

public class GetEventCategoriesListRequest : IRequest<List<EventCategoriesListDto>>
{
}
