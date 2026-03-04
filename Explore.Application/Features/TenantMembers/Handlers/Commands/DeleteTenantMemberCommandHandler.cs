// ABOUTME: Handles tenant member deletion by ID.
// ABOUTME: Returns false if the member is not found.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.TenantMembers.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.TenantMembers.Handlers.Commands;

public class DeleteTenantMemberCommandHandler : IRequestHandler<DeleteTenantMemberCommand, bool>
{
    private readonly ITenantMemberRepository _tenantMemberRepository;

    public DeleteTenantMemberCommandHandler(ITenantMemberRepository tenantMemberRepository)
    {
        _tenantMemberRepository = tenantMemberRepository;
    }

    public async Task<bool> Handle(DeleteTenantMemberCommand request, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantMemberRepository.GetById(request.Id);
        if (tenantMember == null)
        {
            return false;
        }

        await _tenantMemberRepository.Delete(tenantMember);
        return true;
    }
}
