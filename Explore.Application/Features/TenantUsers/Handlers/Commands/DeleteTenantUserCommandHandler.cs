using MediatR;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Features.TenantUsers.Requests.Commands;

namespace Explore.Application.Features.TenantUsers.Handlers.Commands
{
    public class DeleteTenantUserCommandHandler : IRequestHandler<DeleteTenantUserCommand, bool>
    {
        private readonly ITenantUserRepository _tenantUserRepository;

        public DeleteTenantUserCommandHandler(ITenantUserRepository tenantUserRepository)
        {
            _tenantUserRepository = tenantUserRepository;
        }

        public async Task<bool> Handle(DeleteTenantUserCommand request, CancellationToken cancellationToken)
        {
            var tenantUser = await _tenantUserRepository.GetById(request.Id);
            if (tenantUser == null)
            {
                return false;
            }

            await _tenantUserRepository.Delete(tenantUser);
            return true;
        }
    }
}
