// ABOUTME: Handles query for listing all tenant members with eager-loaded navigation properties.
// ABOUTME: Returns mapped list of TenantMemberListDto.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.Features.TenantMembers.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.TenantMembers.Handlers.Queries;

public class GetTenantMemberListRequestHandler : IRequestHandler<GetTenantMemberListRequest, List<TenantMemberListDto>>
{
    private readonly ITenantMemberRepository _tenantMemberRepository;
    private readonly IMapper _mapper;

    public GetTenantMemberListRequestHandler(ITenantMemberRepository tenantMemberRepository, IMapper mapper)
    {
        _tenantMemberRepository = tenantMemberRepository;
        _mapper = mapper;
    }

    public async Task<List<TenantMemberListDto>> Handle(GetTenantMemberListRequest request, CancellationToken cancellationToken)
    {
        var tenantMembers = await _tenantMemberRepository.GetMembersWithDetails();
        return _mapper.Map<List<TenantMemberListDto>>(tenantMembers);
    }
}
