using MediatR;
using System;

namespace Explore.Application.Features.EventTags.Requests.Commands
{
    public class DeleteEventTagsCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
