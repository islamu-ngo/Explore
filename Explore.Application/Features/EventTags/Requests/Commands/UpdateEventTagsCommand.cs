using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTags;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTags.Requests.Commands;

[AuthorizeResource("event", PermissionAction.Update)]
public class UpdateEventTagsCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventTagsDto EventTagsDto { get; set; }

    string? ISecureRequest.ResourceId => EventTagsDto.EventId.ToString();
}
