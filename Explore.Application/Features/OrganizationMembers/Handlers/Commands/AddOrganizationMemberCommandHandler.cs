using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Features.OrganizationMembers.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Explore.Application.Features.OrganizationMembers.Handlers.Commands
{
    public class AddOrganizationMemberCommandHandler : IRequestHandler<AddOrganizationMemberCommand, BaseCommandResponse<Guid>>
    {
        private readonly IOrganizationMemberRepository _organizationMemberRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IMapper _mapper;

        public AddOrganizationMemberCommandHandler(
            IOrganizationMemberRepository organizationMemberRepository,
            IOrganizationRepository organizationRepository,
            IMapper mapper)
        {
            _organizationMemberRepository = organizationMemberRepository;
            _organizationRepository = organizationRepository;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse<Guid>> Handle(AddOrganizationMemberCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse<Guid>();
            var dto = request.AddOrganizationMemberDto;

            // 1. Check if organization exists
            var organization = await _organizationRepository.GetById(dto.OrganizationId);
            if (organization == null)
            {
                response.Success = false;
                response.Message = "Organization not found";
                return response;
            }

            // 2. Check permissions (Requester must be Owner or Admin)
            // First check if requester is the creator (Owner)
            bool isOwner = organization.CreatedByUserId == request.RequesterUserId;
            
            // If not creator, check if they are an Admin member
            if (!isOwner)
            {
                // We need to find the member record for the requester
                // Since we don't have a direct way to query by UserId in the repo yet, we might need to add it or fetch all members
                // For now, let's fetch all members of the org and filter in memory (not efficient but works for small orgs)
                var members = await _organizationMemberRepository.GetMembersByOrganizationId(dto.OrganizationId);
                
                // We need to match UserId (Guid) with RequesterUserId (string). 
                // This assumes we can parse the string to Guid.
                if (Guid.TryParse(request.RequesterUserId, out Guid requesterGuid))
                {
                    var requesterMember = members.FirstOrDefault(m => m.UserId == requesterGuid);
                    if (requesterMember == null || (requesterMember.Role != OrganizationRole.Admin && requesterMember.Role != OrganizationRole.CoOwner && requesterMember.Role != OrganizationRole.Creator))
                    {
                        response.Success = false;
                        response.Message = "You do not have permission to invite members.";
                        return response;
                    }
                }
                else
                {
                     response.Success = false;
                     response.Message = "Invalid requester User ID.";
                     return response;
                }
            }

            // 3. Check if already a member
            var existingMembers = await _organizationMemberRepository.GetMembersByOrganizationId(dto.OrganizationId);
            if (existingMembers.Any(m => m.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase)))
            {
                response.Success = false;
                response.Message = "User with this email is already a member or invited.";
                return response;
            }

            // 4. Create Member
            var organizationMember = new OrganizationMember
            {
                OrganizationId = dto.OrganizationId,
                Email = dto.Email,
                Role = dto.Role,
                UserId = null // Pending invite
            };

            organizationMember = await _organizationMemberRepository.Create(organizationMember);

            response.Success = true;
            response.Message = "Member invited successfully";
            response.Id = organizationMember.Id;

            return response;
        }
    }
}
