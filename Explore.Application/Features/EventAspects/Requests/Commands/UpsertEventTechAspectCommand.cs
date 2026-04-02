// ABOUTME: Command to create or update the Tech aspect for an event.
// ABOUTME: Uses upsert pattern - creates if not exists, updates if exists.

namespace Explore.Application.Features.EventAspects.Requests.Commands;

using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.Responses;
using MediatR;

/// <summary>
/// Command to create or update the Tech aspect for an event.
/// </summary>
[AuthorizeResource("event", AuthorizationActions.Update)]
public class UpsertEventTechAspectCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    /// <summary>
    /// The event ID to attach the Tech aspect to.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// The Tech aspect data to create or update.
    /// </summary>
    public CreateUpdateTechAspectDto AspectDto { get; set; } = null!;

    string? ISecureRequest.ResourceId => EventId.ToString();
}
