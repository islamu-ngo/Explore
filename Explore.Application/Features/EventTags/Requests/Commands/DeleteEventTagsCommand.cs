using System;
using MediatR;

namespace Explore.Application.Features.EventTags.Requests.Commands;

public class DeleteEventTagsCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
