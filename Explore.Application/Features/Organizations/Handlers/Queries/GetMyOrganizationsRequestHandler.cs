using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Organizations.Requests.Queries;
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
            var organizations = await _organizationRepository.GetMyOrganizations(request.UserId);
            return organizations;
        }
    }
}
