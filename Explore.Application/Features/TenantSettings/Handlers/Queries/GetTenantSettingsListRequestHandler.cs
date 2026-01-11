using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantSettings;
using TenantSettingsEntity = Explore.Domain.TenantSettings;
using Explore.Application.Features.TenantSettings.Requests.Queries;
using System.Collections.Generic;

namespace Explore.Application.Features.TenantSettings.Handlers.Queries
{
    public class GetTenantSettingsListRequestHandler : IRequestHandler<GetTenantSettingsListRequest, List<TenantSettingsListDto>>
    {
        private readonly ITenantSettingsRepository _tenantSettingsRepository;
        private readonly IMapper _mapper;

        public GetTenantSettingsListRequestHandler(ITenantSettingsRepository tenantSettingsRepository, IMapper mapper)
        {
            _tenantSettingsRepository = tenantSettingsRepository;
            _mapper = mapper;
        }

        public async Task<List<TenantSettingsListDto>> Handle(GetTenantSettingsListRequest request, CancellationToken cancellationToken)
        {
            var tenantSettings = await _tenantSettingsRepository.GetTenantSettingsListWithDetails();
            return _mapper.Map<List<TenantSettingsListDto>>(tenantSettings);
        }
    }
}
