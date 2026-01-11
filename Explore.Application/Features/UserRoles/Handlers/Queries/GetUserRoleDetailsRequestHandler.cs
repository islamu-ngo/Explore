using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.UserRole;
using Explore.Application.Features.UserRoles.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.UserRoles.Handlers.Queries
{
    public class GetUserRoleDetailsRequestHandler : IRequestHandler<GetUserRoleDetailsRequest, UserRoleDto>
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IMapper _mapper;

        public GetUserRoleDetailsRequestHandler(IUserRoleRepository userRoleRepository, IMapper mapper)
        {
            _userRoleRepository = userRoleRepository;
            _mapper = mapper;
        }

        public async Task<UserRoleDto> Handle(GetUserRoleDetailsRequest request, CancellationToken cancellationToken)
        {
            var userRole = await _userRoleRepository.GetById(request.Id);
            if (userRole == null)
            {
                return null;
            }

            return _mapper.Map<UserRoleDto>(userRole);
        }
    }
}
