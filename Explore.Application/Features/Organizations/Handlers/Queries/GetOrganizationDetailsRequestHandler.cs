using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Organizations.Handlers.Queries
{
    public class GetOrganizationDetailsRequestHandler : IRequestHandler<GetOrganizationDetailsRequest, OrganizationDto>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IMapper _mapper;

        public GetOrganizationDetailsRequestHandler(IOrganizationRepository organizationRepository, IMapper mapper)
        {
            _organizationRepository = organizationRepository;
            _mapper = mapper;
        }
        public async Task<OrganizationDto> Handle(GetOrganizationDetailsRequest request, CancellationToken cancellationToken)
        {
            var organization = await _organizationRepository.GetOrganizationWithDetails(request.Id);
            return _mapper.Map<OrganizationDto>(organization);
        }
    }
}
