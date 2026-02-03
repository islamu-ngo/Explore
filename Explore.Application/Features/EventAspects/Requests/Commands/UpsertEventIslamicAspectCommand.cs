// ABOUTME: Command to create or update the Islamic aspect for an event.
// ABOUTME: Uses upsert pattern - creates if not exists, updates if exists.

namespace Explore.Application.Features.EventAspects.Requests.Commands;

using System;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.Responses;
using MediatR;

/// <summary>
/// Command to create or update the Islamic aspect for an event.
/// </summary>
public class UpsertEventIslamicAspectCommand : IRequest<BaseCommandResponse<Guid>>
{
    /// <summary>
    /// The event ID to attach the Islamic aspect to.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// The Islamic aspect data to create or update.
    /// </summary>
    public CreateUpdateIslamicAspectDto AspectDto { get; set; } = null!;
}
