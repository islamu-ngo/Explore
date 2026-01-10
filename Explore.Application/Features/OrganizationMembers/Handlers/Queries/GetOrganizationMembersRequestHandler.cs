using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Features.OrganizationMembers.Requests.Queries;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Explore.Application.Features.OrganizationMembers.Handlers.Queries
{
    public class GetOrganizationMembersRequestHandler : IRequestHandler<GetOrganizationMembersRequest, List<OrganizationMemberDto>>
    {
        private readonly IOrganizationMemberRepository _organizationMemberRepository;
        private readonly IMapper _mapper;

        public GetOrganizationMembersRequestHandler(IOrganizationMemberRepository organizationMemberRepository, IMapper mapper)
        {
            _organizationMemberRepository = organizationMemberRepository;
            _mapper = mapper;
        }

        public async Task<List<OrganizationMemberDto>> Handle(GetOrganizationMembersRequest request, CancellationToken cancellationToken)
        {
            var members = await _organizationMemberRepository.GetMembersByOrganizationId(request.OrganizationId);
            
            // Manual mapping to avoid AutoMapper navigation property issues
            var result = members.Select(m => new OrganizationMemberDto
            {
                Id = m.Id,
                UserId = m.UserId,
                OrganizationId = m.OrganizationId,
                Role = m.Role,
                Email = m.Email,
                UserName = m.User?.Username,
                ProfilePictureUrl = null // Add this if you have it in the User entity
            }).ToList();
            
            return result;
        }
    }
}
