// ABOUTME: Handler for adding a new member to an organization.
// ABOUTME: Validates authorization, creates the membership record.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Features.OrganizationMembers.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.OrganizationMembers.Handlers.Commands;

public class AddOrganizationMemberCommandHandler : IRequestHandler<AddOrganizationMemberCommand, BaseCommandResponse<Guid>>
{
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public AddOrganizationMemberCommandHandler(
        IOrganizationMemberRepository organizationMemberRepository,
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _organizationMemberRepository = organizationMemberRepository;
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
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

        // 2. Check permissions (Requester must be an Admin member)
        var members = await _organizationMemberRepository.GetMembersByOrganizationId(dto.OrganizationId);

        if (Guid.TryParse(request.RequesterUserId, out Guid requesterGuid))
        {
            var requesterMember = members.FirstOrDefault(m => m.UserId == requesterGuid);
            // Only OrgAdmin role can invite members
            if (requesterMember == null || requesterMember.RoleId != (int)RoleEnum.OrgAdmin)
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

        // 3. Find user by email
        var userToAdd = await _userRepository.GetUserByEmail(dto.Email);
        if (userToAdd == null)
        {
            response.Success = false;
            response.Message = "User with this email not found.";
            return response;
        }

        // 4. Check if user is already a member
        if (members.Any(m => m.UserId == userToAdd.Id))
        {
            response.Success = false;
            response.Message = "User is already a member of this organization.";
            return response;
        }

        // 5. Create Member
        var organizationMember = new OrganizationMember
        {
            OrganizationId = dto.OrganizationId,
            Organization = null!,
            UserId = userToAdd.Id,
            User = null!,
            RoleId = (int)dto.Role,
            Role = null!,
            Tenant = null!
        };

        organizationMember = await _organizationMemberRepository.Create(organizationMember);

        response.Success = true;
        response.Message = "Member added successfully";
        response.Id = organizationMember.Id;

        return response;
    }
}
