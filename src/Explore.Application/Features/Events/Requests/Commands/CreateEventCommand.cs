// ABOUTME: MediatR command for creating a new event.
// ABOUTME: Carries the canonical CreateEventDto graph payload.
using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Create)]
public class CreateEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public const string PreCreateResourceId = "create";
    public const string PreCreateAuthorizationPhase = AuthorizationPhases.PreCreate;

    public required CreateEventDto EventDto { get; set; }

    string? ISecureRequest.ResourceId => PreCreateResourceId;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>
            {
                ["authorizationPhase"] = PreCreateAuthorizationPhase
            };

            if (EventDto.OrganizationId.HasValue)
                attributes["organizationId"] = EventDto.OrganizationId.Value.ToString();

            if (EventDto.GroupId.HasValue)
                attributes["groupId"] = EventDto.GroupId.Value.ToString();

            return attributes;
        }
    }
}
