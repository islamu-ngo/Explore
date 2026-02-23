using System;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Requests.Commands;

[AuthorizeResource("event", PermissionAction.Update)]
public class UpdateEventCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateEventDto EventDto { get; set; }

    string? ISecureRequest.ResourceId => EventDto.Id.ToString();
}
