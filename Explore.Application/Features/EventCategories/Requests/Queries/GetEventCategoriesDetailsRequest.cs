using Explore.Application.DTOs.EventCategories;
using MediatR;
using System;

namespace Explore.Application.Features.EventCategories.Requests.Queries
{
    public class GetEventCategoriesDetailsRequest : IRequest<EventCategoriesDto>
    {
        public Guid Id { get; set; }
    }
}
