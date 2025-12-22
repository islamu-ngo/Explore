using Explore.Application.Responses;
using MediatR;
using System;

namespace Explore.Application.Features.ProgramRegistration.Requests.Commands
{
    public class DeleteProgramRegistrationCommand : IRequest<BaseCommandResponse<object>>
    {
        public Guid RegistrationId { get; set; }
        public Guid UserId { get; set; } // From JWT claims - must own the registration
    }
}
