// ABOUTME: Handler for unified role list query with optional scope filter.
// ABOUTME: Uses IRoleRepository to fetch roles, maps to RoleListDto.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Role;
using Explore.Application.Features.Roles.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Roles.Handlers.Queries;

public class GetRoleListRequestHandler : IRequestHandler<GetRoleListRequest, List<RoleListDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMapper _mapper;

    public GetRoleListRequestHandler(IRoleRepository roleRepository, IMapper mapper)
    {
        _roleRepository = roleRepository;
        _mapper = mapper;
    }

    public async Task<List<RoleListDto>> Handle(GetRoleListRequest request, CancellationToken cancellationToken)
    {
        var roles = request.Scope.HasValue
            ? await _roleRepository.GetByScopeAsync(request.Scope.Value)
            : await _roleRepository.GetAllAsync();

        return _mapper.Map<List<RoleListDto>>(roles);
    }
}
