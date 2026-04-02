// ABOUTME: Command to delete the Islamic aspect from an event.
// ABOUTME: Permanently removes the aspect data.

namespace Explore.Application.Features.EventAspects.Requests.Commands;

using System;
using Explore.Application.Authorization;
using MediatR;

/// <summary>
/// Command to delete the Islamic aspect from an event.
/// </summary>
[AuthorizeResource("event", AuthorizationActions.Update)]
public class DeleteEventIslamicAspectCommand : IRequest<bool>, ISecureRequest
{
    /// <summary>
    /// The event ID to remove the Islamic aspect from.
    /// </summary>
    public Guid EventId { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
