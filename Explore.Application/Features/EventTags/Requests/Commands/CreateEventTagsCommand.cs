// ABOUTME: MediatR command for adding a tag to an event.
// ABOUTME: Carries the CreateEventTagsDto payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTags;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTags.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public class CreateEventTagsCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateEventTagsDto EventTagsDto { get; set; }

    string? ISecureRequest.ResourceId => EventTagsDto.EventId.ToString();
}
