// ABOUTME: Handles CreateFooterLinkCommand — creates a new link inside a footer group.
// ABOUTME: Validates group ownership and auto-assigns Order within the group.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Footer.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Footer.Handlers.Commands;

public sealed class CreateFooterLinkCommandHandler(
    IFooterLinkGroupRepository footerLinkGroupRepository,
    IFooterLinkRepository footerLinkRepository,
    ITenantContext tenantContext,
    FooterLinkMutationGuard mutationGuard)
    : IRequestHandler<CreateFooterLinkCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        CreateFooterLinkCommand request, CancellationToken cancellationToken)
    {
        await mutationGuard.EnsureAllowedAsync(tenantContext.TenantId, cancellationToken);

        var group = await footerLinkGroupRepository.GetById(request.GroupId);

        if (group is null || group.TenantId != tenantContext.TenantId)
            throw new NotFoundException(nameof(group), request.GroupId);

        var maxOrder = await footerLinkRepository.GetMaxOrderInGroupAsync(request.GroupId, cancellationToken);

        var link = new TenantFooterLink
        {
            FooterLinkGroupId = request.GroupId,
            Label = request.Label,
            Url = request.Url,
            OpenInNewTab = request.OpenInNewTab,
            Order = maxOrder + 1,
            IsActive = true,
        };

        link = await footerLinkRepository.Create(link);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = link.Id,
            Message = "Footer link created successfully.",
        };
    }
}
