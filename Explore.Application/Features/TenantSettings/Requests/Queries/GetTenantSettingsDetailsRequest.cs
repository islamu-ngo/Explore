using MediatR;
using Explore.Application.DTOs.TenantSettings;

namespace Explore.Application.Features.TenantSettings.Requests.Queries
{
    public class GetTenantSettingsDetailsRequest : IRequest<TenantSettingsDto>
    {
        public Guid Id { get; set; }
    }
}
