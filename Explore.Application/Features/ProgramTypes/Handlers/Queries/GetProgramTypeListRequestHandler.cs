using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ProgramType;
using Explore.Application.Features.ProgramTypes.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.ProgramTypes.Handlers.Queries
{
    public class GetProgramTypeListRequestHandler : IRequestHandler<GetProgramTypeListRequest, List<ProgramTypeListDto>>
    {
        private readonly IProgramTypeRepository _programTypeRepository;
        private readonly IMapper _mapper;

        public GetProgramTypeListRequestHandler(IProgramTypeRepository programTypeRepository, IMapper mapper)
        {
            _programTypeRepository = programTypeRepository;
            _mapper = mapper;
        }

        public async Task<List<ProgramTypeListDto>> Handle(GetProgramTypeListRequest request, CancellationToken cancellationToken)
        {
            var programTypes = await _programTypeRepository.GetAll();
            return _mapper.Map<List<ProgramTypeListDto>>(programTypes);
        }
    }
}
