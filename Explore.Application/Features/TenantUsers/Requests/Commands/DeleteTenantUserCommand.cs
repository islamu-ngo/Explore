using MediatR;

namespace Explore.Application.Features.TenantUsers.Requests.Commands
{
    public class DeleteTenantUserCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
