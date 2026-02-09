using System;
using System.Collections.Generic;
using Explore.Application.DTOs.Category;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Queries;

public class GetCategoriesByEventRequest : IRequest<List<CategoryListDto>>
{
    public Guid EventId { get; set; }
}
