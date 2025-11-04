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
    public class GetProgramDetailsRequestHandler : IRequestHandler<GetProgramDetailsRequest, ProgramDto>
    {
        private readonly IProgramRepository _programRepository;
        private readonly IMapper _mapper;

        public GetProgramDetailsRequestHandler(IProgramRepository programRepository, IMapper mapper)
        {
            _programRepository = programRepository;
            _mapper = mapper;
        }

        public async Task<ProgramDto> Handle(GetProgramDetailsRequest request, CancellationToken cancellationToken)
        {
            var program = await _programRepository.GetById(request.Id);
            return _mapper.Map<ProgramDto>(program);
        }
    }
}
