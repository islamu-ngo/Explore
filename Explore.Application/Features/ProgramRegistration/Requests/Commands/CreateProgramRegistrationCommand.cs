using Explore.Application.DTOs.ProgramRegistration;
using Explore.Application.Responses;
using MediatR;
using System;

namespace Explore.Application.Features.ProgramRegistration.Requests.Commands
{
    public class CreateProgramRegistrationCommand : IRequest<BaseCommandResponse<Guid>>
    {
        public CreateProgramRegistrationDto ProgramRegistrationDto { get; set; }
        public string UserId { get; set; } = string.Empty; // From JWT claims
    }
}