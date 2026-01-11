using MediatR;
using Explore.Application.DTOs.TenantSettings;
using System.Collections.Generic;

namespace Explore.Application.Features.TenantSettings.Requests.Queries
{
    public class GetTenantSettingsListRequest : IRequest<List<TenantSettingsListDto>>
    {
    }
}
