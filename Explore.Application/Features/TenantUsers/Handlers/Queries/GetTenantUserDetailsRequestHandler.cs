using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Features.TenantUsers.Requests.Queries;

namespace Explore.Application.Features.TenantUsers.Handlers.Queries
{
    public class GetTenantUserDetailsRequestHandler : IRequestHandler<GetTenantUserDetailsRequest, TenantUserDto>
    {
        private readonly ITenantUserRepository _tenantUserRepository;
        private readonly IMapper _mapper;

        public GetTenantUserDetailsRequestHandler(ITenantUserRepository tenantUserRepository, IMapper mapper)
        {
            _tenantUserRepository = tenantUserRepository;
            _mapper = mapper;
        }

        public async Task<TenantUserDto> Handle(GetTenantUserDetailsRequest request, CancellationToken cancellationToken)
        {
            var tenantUser = await _tenantUserRepository.GetTenantUserWithDetails(request.Id);
            if (tenantUser == null)
            {
                return null;
            }

            return _mapper.Map<TenantUserDto>(tenantUser);
        }
    }
}
