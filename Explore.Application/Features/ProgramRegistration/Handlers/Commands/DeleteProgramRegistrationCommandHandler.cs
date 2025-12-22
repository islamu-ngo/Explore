using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ProgramRegistration.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Explore.Application.Features.ProgramRegistration.Handlers.Commands
{
    public class DeleteProgramRegistrationCommandHandler : IRequestHandler<DeleteProgramRegistrationCommand, BaseCommandResponse<object>>
    {
        private readonly IProgramRegistrationRepository _programRegistrationRepository;

        public DeleteProgramRegistrationCommandHandler(IProgramRegistrationRepository programRegistrationRepository)
        {
            _programRegistrationRepository = programRegistrationRepository;
        }

        public async Task<BaseCommandResponse<object>> Handle(DeleteProgramRegistrationCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<object>();

            try
            {
                // Get the registration
                var registration = await _programRegistrationRepository.GetById(request.RegistrationId);
                
                if (registration == null)
                {
                    response.Success = false;
                    response.Message = "Registration not found.";
                    return response;
                }

                // Verify ownership: user can only delete their own registration
                if (registration.UserId != request.UserId)
                {
                    response.Success = false;
                    response.Message = "You do not have permission to delete this registration.";
                    return response;
                }

                // Delete the registration
                await _programRegistrationRepository.Delete(registration);

                response.Success = true;
                response.Message = "Registration deleted successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error deleting registration.";
                response.Errors = new List<string> { ex.Message };
            }

            return response;
        }
    }
}
