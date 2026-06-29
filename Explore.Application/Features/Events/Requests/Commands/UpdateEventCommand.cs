// ABOUTME: MediatR command for PATCH-based Event property updates.
// ABOUTME: Carries route authority, If-Match concurrency, and a grouped update payload.

using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Update)]
public class UpdateEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; set; }

    public Guid ExpectedConcurrencyStamp { get; set; }

    public required UpdateEventDto UpdateEventDto { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
