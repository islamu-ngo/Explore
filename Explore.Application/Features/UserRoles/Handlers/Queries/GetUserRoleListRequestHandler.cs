using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserRole;
using Explore.Application.Features.UserRoles.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.UserRoles.Handlers.Queries;

public class GetUserRoleListRequestHandler : IRequestHandler<GetUserRoleListRequest, List<UserRoleListDto>>
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IMapper _mapper;

    public GetUserRoleListRequestHandler(IUserRoleRepository userRoleRepository, IMapper mapper)
    {
        _userRoleRepository = userRoleRepository;
        _mapper = mapper;
    }

    public async Task<List<UserRoleListDto>> Handle(GetUserRoleListRequest request, CancellationToken cancellationToken)
    {
        var userRoles = await _userRoleRepository.GetAll();
        return _mapper.Map<List<UserRoleListDto>>(userRoles);
    }
}
