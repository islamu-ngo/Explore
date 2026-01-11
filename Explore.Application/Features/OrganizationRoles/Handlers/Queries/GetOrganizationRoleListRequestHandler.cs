using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.OrganizationRole;
using Explore.Application.Features.OrganizationRoles.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.OrganizationRoles.Handlers.Queries
{
    public class GetOrganizationRoleListRequestHandler : IRequestHandler<GetOrganizationRoleListRequest, List<OrganizationRoleListDto>>
    {
        private readonly IOrganizationRoleRepository _organizationRoleRepository;
        private readonly IMapper _mapper;

        public GetOrganizationRoleListRequestHandler(IOrganizationRoleRepository organizationRoleRepository, IMapper mapper)
        {
            _organizationRoleRepository = organizationRoleRepository;
            _mapper = mapper;
        }

        public async Task<List<OrganizationRoleListDto>> Handle(GetOrganizationRoleListRequest request, CancellationToken cancellationToken)
        {
            var organizationRoles = await _organizationRoleRepository.GetAll();
            return _mapper.Map<List<OrganizationRoleListDto>>(organizationRoles);
        }
    }
}
