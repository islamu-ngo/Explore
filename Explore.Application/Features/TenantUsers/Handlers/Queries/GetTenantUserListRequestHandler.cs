using System.Collections.Generic;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Features.TenantUsers.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.TenantUsers.Handlers.Queries;

public class GetTenantUserListRequestHandler : IRequestHandler<GetTenantUserListRequest, List<TenantUserListDto>>
{
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IMapper _mapper;

    public GetTenantUserListRequestHandler(ITenantUserRepository tenantUserRepository, IMapper mapper)
    {
        _tenantUserRepository = tenantUserRepository;
        _mapper = mapper;
    }

    public async Task<List<TenantUserListDto>> Handle(GetTenantUserListRequest request, CancellationToken cancellationToken)
    {
        var tenantUsers = await _tenantUserRepository.GetTenantUsersWithDetails();
        return _mapper.Map<List<TenantUserListDto>>(tenantUsers);
    }
}
