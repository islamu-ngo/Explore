// ABOUTME: Handles query for tenant member details by ID with eager-loaded navigation properties.
// ABOUTME: Returns null if the member is not found.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.Features.TenantMembers.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.TenantMembers.Handlers.Queries;

public class GetTenantMemberDetailsRequestHandler : IRequestHandler<GetTenantMemberDetailsRequest, TenantMemberDto>
{
    private readonly ITenantMemberRepository _tenantMemberRepository;
    private readonly IMapper _mapper;

    public GetTenantMemberDetailsRequestHandler(ITenantMemberRepository tenantMemberRepository, IMapper mapper)
    {
        _tenantMemberRepository = tenantMemberRepository;
        _mapper = mapper;
    }

    public async Task<TenantMemberDto> Handle(GetTenantMemberDetailsRequest request, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantMemberRepository.GetMemberWithDetails(request.Id);
        if (tenantMember == null)
        {
            return null;
        }

        return _mapper.Map<TenantMemberDto>(tenantMember);
    }
}
