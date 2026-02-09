using System;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Commands;

public class CreateEventSessionCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required CreateEventSessionDto EventSessionDto { get; set; }
}
