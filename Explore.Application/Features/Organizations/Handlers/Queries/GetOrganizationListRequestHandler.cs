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
    public class GetOrganizationListRequestHandler : IRequestHandler<GetOrganizationListRequest, List<OrganizationListDto>>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IMapper _mapper;

        public GetOrganizationListRequestHandler(IOrganizationRepository organizationRepository, IMapper mapper)
        {
            _organizationRepository = organizationRepository;
            _mapper = mapper;
        }

        public async Task<List<OrganizationListDto>> Handle(GetOrganizationListRequest request, CancellationToken cancellationToken)
        {
            // Get organizations with StatusType for admin purposes
            var organizations = await _organizationRepository.GetOrganizationsWithDetails();
            return _mapper.Map<List<OrganizationListDto>>(organizations);
        }
    }
}
