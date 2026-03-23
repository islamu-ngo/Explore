// ABOUTME: Query handler returning all organizations the user belongs to.
// ABOUTME: Filters by user ID, maps to OrganizationListDto.
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Organization;
using Explore.Application.Features.Users.Requests.Queries;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Users.Handlers.Queries;

/// <summary>
/// Handler to get all organizations a user is a member of.
/// Uses the OrganizationMember table to find memberships.
/// </summary>
public class GetUserOrganizationsRequestHandler : IRequestHandler<GetUserOrganizationsRequest, List<OrganizationListDto>>
{
    private readonly IOrganizationMemberRepository _organizationMemberRepository;
    private readonly IMapper _mapper;

    public GetUserOrganizationsRequestHandler(
        IOrganizationMemberRepository organizationMemberRepository,
        IMapper mapper)
    {
        _organizationMemberRepository = organizationMemberRepository;
        _mapper = mapper;
    }

    public async Task<List<OrganizationListDto>> Handle(GetUserOrganizationsRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[GET USER ORGS] Getting organizations for user: {request.UserId}");

        // Get all organization memberships for this user
        var memberships = await _organizationMemberRepository.GetMembershipsByUser(request.UserId);

        Console.WriteLine($"[GET USER ORGS] Found {memberships.Count} memberships");

        // Map to DTOs and include the user's role in each organization
        var dtos = new List<OrganizationListDto>();

        foreach (var membership in memberships)
        {
            if (membership.Organization == null) continue;

            var dto = _mapper.Map<OrganizationListDto>(membership.Organization);
            dto.CurrentUserRole = (RoleEnum)membership.RoleId;
            dtos.Add(dto);

            Console.WriteLine($"[GET USER ORGS] Org: {dto.FullName}, Role: {dto.CurrentUserRole}");
        }

        return dtos;
    }
}
