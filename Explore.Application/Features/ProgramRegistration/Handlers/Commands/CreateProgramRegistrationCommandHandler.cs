using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ProgramRegistration;
using Explore.Application.Features.ProgramRegistration.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Explore.Application.Features.ProgramRegistration.Handlers.Commands
{
    public class CreateProgramRegistrationCommandHandler : IRequestHandler<CreateProgramRegistrationCommand, BaseCommandResponse<Guid>>
    {
        private readonly IProgramRegistrationRepository _programRegistrationRepository;
        private readonly IMapper _mapper;

        public CreateProgramRegistrationCommandHandler(
            IProgramRegistrationRepository programRegistrationRepository, 
            IMapper mapper)
        {
            _programRegistrationRepository = programRegistrationRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateProgramRegistrationCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            try
            {
                var userId = Guid.Parse(request.UserId);
                var programId = request.ProgramRegistrationDto.ProgramId;

                // Check if user is already registered
                var isAlreadyRegistered = await _programRegistrationRepository.IsUserAlreadyRegisteredAsync(userId, programId);
                if (isAlreadyRegistered)
                {
                    response.Success = false;
                    response.Message = "You are already registered for this event.";
                    return response;
                }

                // Map DTO to domain entity
                var registration = new ProgramRegistartion
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProgramId = programId,
                    StatusTypeId = 2, // 2 = Active (auto-approved)
                    FirstName = request.ProgramRegistrationDto.FirstName,
                    LastName = request.ProgramRegistrationDto.LastName,
                    Email = request.ProgramRegistrationDto.Email
                };

                // Save to database
                var createdRegistration = await _programRegistrationRepository.Create(registration);

                response.Success = true;
                response.Id = createdRegistration.Id;
                response.Message = "Program registration created successfully.";
                
                // Note: Form details (name, email, etc.) are collected for UX but not persisted
                // They could be logged, emailed, or stored elsewhere if needed
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Registration failed.";
                response.Errors = new List<string> { ex.Message };
            }

            return response;
        }
    }
}