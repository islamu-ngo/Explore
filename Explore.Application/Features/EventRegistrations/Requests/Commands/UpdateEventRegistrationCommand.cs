using System;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Requests.Commands;

public class UpdateEventRegistrationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateEventRegistrationDto EventRegistrationDto { get; set; }
}
