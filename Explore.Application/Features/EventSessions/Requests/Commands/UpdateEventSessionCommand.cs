using System;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

public class UpdateEventSessionCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateEventSessionDto EventSessionDto { get; set; }
}
