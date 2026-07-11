// ABOUTME: MediatR command for removing a tag from an event.
// ABOUTME: Carries the junction record ID.
using System;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.EventTags.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public class DeleteEventTagsCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
