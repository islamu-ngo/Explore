using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ProgramRegistration.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.ProgramRegistration.Handlers.Queries
{
    public class CheckUserRegistrationStatusRequestHandler : IRequestHandler<CheckUserRegistrationStatusRequest, bool>
    {
        private readonly IProgramRegistrationRepository _programRegistrationRepository;

        public CheckUserRegistrationStatusRequestHandler(IProgramRegistrationRepository programRegistrationRepository)
        {
            _programRegistrationRepository = programRegistrationRepository;
        }

        public async Task<bool> Handle(CheckUserRegistrationStatusRequest request, CancellationToken cancellationToken)
        {
            return await _programRegistrationRepository.IsUserAlreadyRegisteredAsync(request.UserId, request.ProgramId);
        }
    }
}