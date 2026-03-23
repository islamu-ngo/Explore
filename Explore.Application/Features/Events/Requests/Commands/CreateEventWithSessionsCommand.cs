// ABOUTME: MediatR command for creating an event with initial sessions.
// ABOUTME: Carries CreateEventWithSessionsDto.
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

/// <summary>
/// Command to create an event along with its sessions in a single transaction.
/// FirstSessionDate and LastSessionDate are computed from the provided sessions.
/// </summary>
public class CreateEventWithSessionsCommand : IRequest<BaseCommandResponse<Guid>>
{
    /// <summary>
    /// The event and sessions data.
    /// </summary>
    public required CreateEventWithSessionsDto EventWithSessionsDto { get; set; }
}
