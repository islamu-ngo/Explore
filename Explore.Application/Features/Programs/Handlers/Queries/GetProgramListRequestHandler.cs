using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Program;
using Explore.Application.Features.Programs.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Programs.Handlers.Queries
{
    public class GetProgramListRequestHandler : IRequestHandler<GetProgramListRequest, List<ProgramListDto>>
    {
        private readonly IProgramRepository _programRepository;
        private readonly IMapper _mapper;

        public GetProgramListRequestHandler(IProgramRepository programRepository, IMapper mapper)
        {
            _programRepository = programRepository;
            _mapper = mapper;
        }

        public async Task<List<ProgramListDto>> Handle(GetProgramListRequest request, CancellationToken cancellationToken)
        {
            var programs = await _programRepository.GetAll();
            return _mapper.Map<List<ProgramListDto>>(programs);
        }
    }
}
