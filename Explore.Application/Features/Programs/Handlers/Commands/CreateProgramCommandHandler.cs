using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Programs.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Programs.Handlers.Commands
{
    public class CreateProgramCommandHandler : IRequestHandler<CreateProgramCommand, BaseCommandResponse<Guid>>
    {
        //private readonly IProgramRepository _programRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IEducationRepository _educationRepository;
        private readonly IMapper _mapper;

        public CreateProgramCommandHandler(IEventRepository eventRepository, IEducationRepository educationRepository, IMapper mapper)
        {
            _eventRepository = eventRepository;
            _educationRepository = educationRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(CreateProgramCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();

            //var validator = new CreateProgramDtoValidator();
            //var validationResult = await validator.ValidateAsync(request.OrganizationDto);

            //if (!validationResult.IsValid)
            //{
            //    response.Success = false;
            //    response.Message = "Program creation failed.";
            //    response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            //    return response;
            //}

            if (request.ProgramDto.ProgramTypeId == (int)ProgramTypeEnum.Event)
            {
                // "event" is a reserved keyword in c# so added @. I could use another name like eventEntity but for consistency i keep name as classname in lowercase
                var @event = _mapper.Map<Event>(request.ProgramDto);
                @event = await _eventRepository.Create(@event);
                
                response.Success = true;
                response.Id = @event.Id;
                response.Message = "Event created successfully.";
            }
            else if (request.ProgramDto.ProgramTypeId == (int)ProgramTypeEnum.Education)
            {
                var education = _mapper.Map<Education>(request.ProgramDto);
                education = await _educationRepository.Create(education);

                response.Success = true;
                response.Id = education.Id;
                response.Message = "Education program created successfully.";

            }
            else
            {
                response.Success = false;
                response.Message = $"Invalid ProgramTypeId: {request.ProgramDto.ProgramTypeId}. Must be Event (1) or Education (2).";
            }

            //var program = _mapper.Map<Program>(request.ProgramDto);
            //program = await _programRepository.Create(program);

            //response.Success = true;
            //response.Message = "Program created successfully.";
            //response.Id = program.Id;
            //return response;
            return response;
        }
    }
}
