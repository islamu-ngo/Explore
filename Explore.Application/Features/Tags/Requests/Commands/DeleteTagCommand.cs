using System;
using MediatR;

namespace Explore.Application.Features.Tags.Requests.Commands
{
    public class DeleteTagCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
