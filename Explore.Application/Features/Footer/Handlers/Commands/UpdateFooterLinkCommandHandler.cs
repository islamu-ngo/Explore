// ABOUTME: Handles UpdateFooterLinkCommand — updates label, URL, and display options.
// ABOUTME: Validates parent group ownership before persisting.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Footer.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Handlers.Commands;

public sealed class UpdateFooterLinkCommandHandler(
    IFooterLinkGroupRepository footerLinkGroupRepository,
    IFooterLinkRepository footerLinkRepository,
    ITenantContext tenantContext)
    : IRequestHandler<UpdateFooterLinkCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateFooterLinkCommand request, CancellationToken cancellationToken)
    {
        var link = await footerLinkRepository.GetById(request.LinkId);
        if (link is null)
            throw new NotFoundException(nameof(link), request.LinkId);

        var group = await footerLinkGroupRepository.GetById(link.FooterLinkGroupId);
        if (group is null || group.TenantId != tenantContext.TenantId)
            throw new NotFoundException(nameof(group), link.FooterLinkGroupId);

        link.Label = request.Label;
        link.Url = request.Url;
        link.OpenInNewTab = request.OpenInNewTab;
        link.IsActive = request.IsActive;

        await footerLinkRepository.Update(link);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = link.Id,
            Message = "Footer link updated successfully.",
        };
    }
}
