using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ProgramRegistration;
using Explore.Application.Features.ProgramRegistration.Requests.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Explore.Application.Features.ProgramRegistration.Handlers.Queries
{
    public class GetMyProgramRegistrationsRequestHandler : IRequestHandler<GetMyProgramRegistrationsRequest, List<ProgramRegistrationListDto>>
    {
        private readonly IProgramRegistrationRepository _programRegistrationRepository;

        public GetMyProgramRegistrationsRequestHandler(IProgramRegistrationRepository programRegistrationRepository)
        {
            _programRegistrationRepository = programRegistrationRepository;
        }

        public async Task<List<ProgramRegistrationListDto>> Handle(GetMyProgramRegistrationsRequest request, CancellationToken cancellationToken)
        {
            var registrations = await _programRegistrationRepository.GetRegistrationsForUserAsync(request.UserId);

            if (!registrations.Any())
            {
                return new List<ProgramRegistrationListDto>();
            }

            return registrations.Select(r => new ProgramRegistrationListDto
            {
                Id = r.Id,
                ProgramId = r.ProgramId,
                UserId = r.UserId,
                UserName = !string.IsNullOrEmpty(r.FirstName)
                    ? $"{r.FirstName} {r.LastName}".Trim()
                    : "",
                UserEmail = r.Email ?? string.Empty,
                RegistrationDate = DateTime.UtcNow, // TODO: add CreatedAt to ProgramRegistartion
                Status = r.StatusType?.FullName ?? "Unknown",
                ProgramTitle = r.Program?.Title ?? string.Empty,
                ProgramDescription = r.Program?.Description ?? string.Empty,
                ProgramCity = r.Program?.City,
                ProgramAddress = r.Program?.Address,
                ProgramUrl = r.Program?.ProgramUrl,
                EventStartDate = r.Program?.StartDate,
                EventEndDate = r.Program?.EndDate,
                OrganizationId = r.Program?.OrganizationId ?? Guid.Empty,
                OrganizationName = r.Program?.Organization?.FullName ?? string.Empty
            }).ToList();
        }
    }
}
