using MediatR;

namespace Explore.Application.Features.TenantSettings.Requests.Commands
{
    public class DeleteTenantSettingsCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
