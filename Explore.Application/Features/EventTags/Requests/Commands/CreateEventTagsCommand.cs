using System;
using Explore.Application.DTOs.EventTags;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTags.Requests.Commands;

public class CreateEventTagsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateEventTagsDto EventTagsDto { get; set; }
}
