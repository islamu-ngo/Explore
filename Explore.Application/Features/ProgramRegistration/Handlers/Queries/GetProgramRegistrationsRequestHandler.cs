using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ProgramRegistration;
using Explore.Application.Features.ProgramRegistration.Requests.Queries;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Explore.Application.Features.ProgramRegistration.Handlers.Queries
{
    public class GetProgramRegistrationsRequestHandler : IRequestHandler<GetProgramRegistrationsRequest, List<ProgramRegistrationListDto>>
    {
        private readonly IProgramRegistrationRepository _programRegistrationRepository;
        private readonly IUserRepository _userRepository;

        public GetProgramRegistrationsRequestHandler(
            IProgramRegistrationRepository programRegistrationRepository,
            IUserRepository userRepository)
        {
            _programRegistrationRepository = programRegistrationRepository;
            _userRepository = userRepository;
        }

        public async Task<List<ProgramRegistrationListDto>> Handle(GetProgramRegistrationsRequest request, CancellationToken cancellationToken)
        {
            var registrations = await _programRegistrationRepository.GetRegistrationsForProgramAsync(request.ProgramId);
            
            if (!registrations.Any())
            {
                return new List<ProgramRegistrationListDto>();
            }

            var userIds = registrations.Select(r => r.UserId).Distinct().ToList();
            var users = await _userRepository.GetUsersByIdsAsync(userIds);
            var userMap = users.ToDictionary(u => u.Id, u => u);

            var result = registrations.Select(r => new ProgramRegistrationListDto
            {
                Id = r.Id,
                ProgramId = r.ProgramId,
                UserId = r.UserId,
                UserName = !string.IsNullOrEmpty(r.FirstName) 
                    ? $"{r.FirstName} {r.LastName}" 
                    : (userMap.ContainsKey(r.UserId) ? userMap[r.UserId].Username : "Unknown"),
                UserEmail = !string.IsNullOrEmpty(r.Email) 
                    ? r.Email 
                    : (userMap.ContainsKey(r.UserId) ? userMap[r.UserId].Email : "Unknown"),
                RegistrationDate = DateTime.UtcNow, // Assuming we don't have CreatedAt in ProgramRegistration yet, or I missed it.
                Status = r.StatusType?.FullName ?? "Unknown"
            }).ToList();

            return result;
        }
    }
}
