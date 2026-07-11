// ABOUTME: Query request to get the Tech aspect for an event.
// ABOUTME: Returns null if the event doesn't have a Tech aspect.

namespace Explore.Application.Features.EventAspects.Requests.Queries;

using System;
using Explore.Application.DTOs.EventAspects;
using MediatR;

/// <summary>
/// Request to retrieve the Tech aspect for a specific event.
/// </summary>
public class GetEventTechAspectRequest : IRequest<EventTechAspectDto?>
{
    /// <summary>
    /// The event ID to get the Tech aspect for.
    /// </summary>
    public Guid EventId { get; set; }
}
