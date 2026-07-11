// ABOUTME: Handles UpdateFooterLinkGroupCommand — updates title and active state.
// ABOUTME: Validates group ownership before persisting changes.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Footer.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Handlers.Commands;

public sealed class UpdateFooterLinkGroupCommandHandler(
    IFooterLinkGroupRepository footerLinkGroupRepository,
    ITenantContext tenantContext)
    : IRequestHandler<UpdateFooterLinkGroupCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateFooterLinkGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await footerLinkGroupRepository.GetById(request.GroupId);

        if (group is null || group.TenantId != tenantContext.TenantId)
            throw new NotFoundException(nameof(group), request.GroupId);

        group.Title = request.Title;
        group.IsActive = request.IsActive;

        await footerLinkGroupRepository.Update(group);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = group.Id,
            Message = "Footer link group updated successfully.",
        };
    }
}
