using Explore.Application.DTOs.Tag;
using MediatR;
using System;
using System.Collections.Generic;

namespace Explore.Application.Features.EventTags.Requests.Queries
{
    public class GetTagsByEventRequest : IRequest<List<TagListDto>>
    {
        public Guid EventId { get; set; }
    }
}
