using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Requests.Queries;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Organizations.Handlers.Queries
{
    public class GetMyOrganizationsRequestHandler : IRequestHandler<GetMyOrganizationsRequest, List<OrganizationListDto>>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IMapper _mapper;

        public GetMyOrganizationsRequestHandler(IOrganizationRepository organizationRepository, IMapper mapper)
        {
            _organizationRepository = organizationRepository;
            _mapper = mapper;
        }

        public async Task<List<OrganizationListDto>> Handle(GetMyOrganizationsRequest request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.UserId, out Guid userGuid))
            {
                return new List<OrganizationListDto>();
            }

            var organizations = await _organizationRepository.GetMyOrganizations(userGuid);
            var dtos = _mapper.Map<List<OrganizationListDto>>(organizations);

            // Populate CurrentUserRole for each organization based on the user's membership
            foreach (var dto in dtos)
            {
                var org = organizations.FirstOrDefault(o => o.Id == dto.Id);
                if (org != null)
                {
                    var userMembership = org.Members?.FirstOrDefault(m => m.UserId == userGuid);
                    if (userMembership != null)
                    {
                        dto.CurrentUserRole = (OrganizationRoleEnum)userMembership.OrganizationRoleId;
                    }
                }
            }

            return dtos;
        }
    }
}
