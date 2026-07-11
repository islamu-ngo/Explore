// ABOUTME: MediatR command for creating a new event.
// ABOUTME: Carries the canonical CreateEventRequest graph payload.
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

    public required CreateEventRequest Request { get; set; }

    string? ISecureRequest.ResourceId => PreCreateResourceId;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>
            {
                ["authorizationPhase"] = PreCreateAuthorizationPhase
            };

            if (Request.OrganizationId.HasValue)
                attributes["organizationId"] = Request.OrganizationId.Value.ToString();

            if (Request.GroupId.HasValue)
                attributes["groupId"] = Request.GroupId.Value.ToString();

            return attributes;
        }
    }
}
