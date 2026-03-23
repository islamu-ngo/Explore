// ABOUTME: MediatR query request for fetching a single event-category link by ID.
// ABOUTME: Returns EventCategoriesDto.
using System;
using Explore.Application.DTOs.EventCategories;
using MediatR;

namespace Explore.Application.Features.EventCategories.Requests.Queries;

public class GetEventCategoriesDetailsRequest : IRequest<EventCategoriesDto>
{
    public Guid Id { get; set; }
}
