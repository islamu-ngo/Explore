// ABOUTME: MediatR query for fetching all categories on a given event.
// ABOUTME: Returns IEnumerable<CategoryDto>.
using System;
using System.Collections.Generic;
using Explore.Application.DTOs.Category;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Queries;

public class GetCategoriesByEventRequest : IRequest<List<CategoryListDto>>
{
    public Guid EventId { get; set; }
}
