using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Organizations.Handlers.Queries
{
    public class GetOrganizationListRequestHandler : IRequestHandler<GetOrganizationListRequest, PaginatedResult<OrganizationListDto>>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IMapper _mapper;

        public GetOrganizationListRequestHandler(IOrganizationRepository organizationRepository, IMapper mapper)
        {
            _organizationRepository = organizationRepository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<OrganizationListDto>> Handle(GetOrganizationListRequest request, CancellationToken cancellationToken)
        {
            // Get organizations with ApprovalStatus for admin purposes
            var (organizations, totalCount) = await _organizationRepository.GetOrganizationsWithDetailsPaged(request.PageNumber, request.PageSize);
            var organizationDtos = _mapper.Map<List<OrganizationListDto>>(organizations);

            return PaginatedResult<OrganizationListDto>.Create(organizationDtos, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
