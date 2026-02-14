// ABOUTME: Handler for listing permissions with scope, group, and filter options.
// ABOUTME: Uses PermissionRegistryService for cached lookups.

using AutoMapper;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Permission;
using Explore.Application.Features.Permissions.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Permissions.Handlers.Queries;

public class GetPermissionListRequestHandler : IRequestHandler<GetPermissionListRequest, List<PermissionListDto>>
{
    private readonly IPermissionRegistryService _permissionRegistry;
    private readonly IMapper _mapper;

    public GetPermissionListRequestHandler(
        IPermissionRegistryService permissionRegistry,
        IMapper mapper)
    {
        _permissionRegistry = permissionRegistry;
        _mapper = mapper;
    }

    public async Task<List<PermissionListDto>> Handle(GetPermissionListRequest request, CancellationToken cancellationToken)
    {
        var permissions = await _permissionRegistry.GetFilteredPermissionsAsync(
            request.Scope,
            request.ExcludeFiltered);

        if (!string.IsNullOrEmpty(request.GroupName))
        {
            permissions = permissions
                .Where(p => p.GroupName == request.GroupName)
                .ToList()
                .AsReadOnly();
        }

        return _mapper.Map<List<PermissionListDto>>(permissions);
    }
}
