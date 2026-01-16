using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Responses;
using MediatR;
using System;

namespace Explore.Application.Features.EventRegistrations.Requests.Commands
{
    public class UpdateEventRegistrationCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public UpdateEventRegistrationDto EventRegistrationDto { get; set; }
    }
}
