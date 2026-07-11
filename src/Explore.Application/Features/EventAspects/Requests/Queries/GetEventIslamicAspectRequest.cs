// ABOUTME: Query request to get the Islamic aspect for an event.
// ABOUTME: Returns null if the event doesn't have an Islamic aspect.

namespace Explore.Application.Features.EventAspects.Requests.Queries;

using System;
using Explore.Application.DTOs.EventAspects;
using MediatR;

/// <summary>
/// Request to retrieve the Islamic aspect for a specific event.
/// </summary>
public class GetEventIslamicAspectRequest : IRequest<EventIslamicAspectDto?>
{
    /// <summary>
    /// The event ID to get the Islamic aspect for.
    /// </summary>
    public Guid EventId { get; set; }
}
