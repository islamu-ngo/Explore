// ABOUTME: Single command for all event updates using the null-check DTO pattern.
// ABOUTME: Each nullable DTO targets a specific update; the handler applies whichever is non-null.

using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource("event", AuthorizationActions.Update)]
public class UpdateEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; set; }

    public UpdateEventDto? EventDto { get; set; }
    public UpdateEventStatusDto? EventStatusDto { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
