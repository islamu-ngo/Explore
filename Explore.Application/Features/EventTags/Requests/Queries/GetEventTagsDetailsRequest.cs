using Explore.Application.DTOs.EventTags;
using MediatR;
using System;

namespace Explore.Application.Features.EventTags.Requests.Queries
{
    public class GetEventTagsDetailsRequest : IRequest<EventTagsDto>
    {
        public Guid Id { get; set; }
    }
}
