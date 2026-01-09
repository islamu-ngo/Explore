using System;
using Explore.Application.DTOs.Tag;
using MediatR;

namespace Explore.Application.Features.Tags.Requests.Queries
{
    public class GetTagDetailsRequest : IRequest<TagDto>
    {
        public Guid Id { get; set; }
    }
}
