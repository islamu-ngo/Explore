using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.OrganizationRole;
using Explore.Application.Features.OrganizationRoles.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.OrganizationRoles.Handlers.Queries
{
    public class GetOrganizationRoleDetailsRequestHandler : IRequestHandler<GetOrganizationRoleDetailsRequest, OrganizationRoleDto>
    {
        private readonly IOrganizationRoleRepository _organizationRoleRepository;
        private readonly IMapper _mapper;

        public GetOrganizationRoleDetailsRequestHandler(IOrganizationRoleRepository organizationRoleRepository, IMapper mapper)
        {
            _organizationRoleRepository = organizationRoleRepository;
            _mapper = mapper;
        }

        public async Task<OrganizationRoleDto> Handle(GetOrganizationRoleDetailsRequest request, CancellationToken cancellationToken)
        {
            var organizationRole = await _organizationRoleRepository.GetById(request.Id);
            return _mapper.Map<OrganizationRoleDto>(organizationRole);
        }
    }
}
