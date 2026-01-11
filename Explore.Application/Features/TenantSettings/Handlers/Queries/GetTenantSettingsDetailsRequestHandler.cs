using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Features.TenantSettings.Requests.Queries;

namespace Explore.Application.Features.TenantSettings.Handlers.Queries
{
    public class GetTenantSettingsDetailsRequestHandler : IRequestHandler<GetTenantSettingsDetailsRequest, TenantSettingsDto>
    {
        private readonly ITenantSettingsRepository _tenantSettingsRepository;
        private readonly IMapper _mapper;

        public GetTenantSettingsDetailsRequestHandler(ITenantSettingsRepository tenantSettingsRepository, IMapper mapper)
        {
            _tenantSettingsRepository = tenantSettingsRepository;
            _mapper = mapper;
        }

        public async Task<TenantSettingsDto> Handle(GetTenantSettingsDetailsRequest request, CancellationToken cancellationToken)
        {
            var tenantSettings = await _tenantSettingsRepository.GetTenantSettingsWithDetails(request.Id);
            if (tenantSettings == null)
            {
                return null;
            }

            return _mapper.Map<TenantSettingsDto>(tenantSettings);
        }
    }
}
