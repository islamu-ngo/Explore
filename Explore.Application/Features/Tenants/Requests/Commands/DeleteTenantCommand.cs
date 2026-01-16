using MediatR;

namespace Explore.Application.Features.Tenants.Requests.Commands
{
    public class DeleteTenantCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
