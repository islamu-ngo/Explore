using System;
using Explore.Application.DTOs.EventCategories;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Queries;

public class GetEventCategoriesDetailsRequest : IRequest<EventCategoriesDto>
{
    public Guid Id { get; set; }
}
