using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Program;
using Explore.Application.Features.Programs.Requests.Queries;
using MediatR;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Features.Programs.Handlers.Queries
{
    public class GetProgramDetailsRequestHandler : IRequestHandler<GetProgramDetailsRequest, ProgramDto>
    {
        private readonly IProgramRepository _programRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IEducationRepository _educationRepository;
        private readonly IMapper _mapper;

        public GetProgramDetailsRequestHandler(
            IProgramRepository programRepository,
            IEventRepository eventRepository,
            IEducationRepository educationRepository,
            IMapper mapper)
        {
            _programRepository = programRepository;
            _eventRepository = eventRepository;
            _educationRepository = educationRepository;
            _mapper = mapper;
        }

        public async Task<ProgramDto> Handle(GetProgramDetailsRequest request, CancellationToken cancellationToken)
        {
            var program = await _programRepository.GetById(request.Id);
            
            if (program == null)
            {
                return new ProgramDto(); // Return empty DTO instead of null
            }

            var programDto = _mapper.Map<ProgramDto>(program);

            // Check if it's an Event (ProgramTypeId = 1)
            if (program.ProgramTypeId == (int)ProgramTypeEnum.Event)
            {
                var eventEntity = await _eventRepository.GetById(request.Id);
                if (eventEntity != null)
                {
                    programDto.Event = _mapper.Map<Explore.Application.DTOs.Event.EventDto>(eventEntity);
                }
            }
            // Check if it's an Education (ProgramTypeId = 2)
            else if (program.ProgramTypeId == (int)ProgramTypeEnum.Education)
            {
                var educationEntity = await _educationRepository.GetById(request.Id);
                if (educationEntity != null)
                {
                    programDto.Education = _mapper.Map<Explore.Application.DTOs.Education.EducationDto>(educationEntity);
                }
            }

            return programDto;
        }
    }
}
