using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Responses;
using MediatR;
using System;

namespace Explore.Application.Features.EventRegistrations.Requests.Commands
{
    public class CreateEventRegistrationCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateEventRegistrationDto EventRegistrationDto { get; set; }
    }
}
