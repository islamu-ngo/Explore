using System;
using Explore.Application.DTOs.EventTags;
using MediatR;

namespace Explore.Application.Features.EventTags.Requests.Queries;

public class GetEventTagsDetailsRequest : IRequest<EventTagsDto>
{
    public Guid Id { get; set; }
}
