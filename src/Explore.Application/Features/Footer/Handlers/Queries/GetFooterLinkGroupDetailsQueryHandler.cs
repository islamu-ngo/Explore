// ABOUTME: Handles GetFooterLinkGroupDetailsQuery — returns a group with child links for admin edit.
// ABOUTME: Returns null response (NotFoundException) when the group is not owned by the current tenant.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Footer;
using Explore.Application.Exceptions;
using Explore.Application.Features.Footer.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Footer.Handlers.Queries;

public sealed class GetFooterLinkGroupDetailsQueryHandler(
    IFooterLinkGroupRepository footerLinkGroupRepository,
    ITenantContext tenantContext,
    IMapper mapper)
    : IRequestHandler<GetFooterLinkGroupDetailsQuery, FooterLinkGroupDetailsDto>
{
    public async Task<FooterLinkGroupDetailsDto> Handle(
        GetFooterLinkGroupDetailsQuery request, CancellationToken cancellationToken)
    {
        var group = await footerLinkGroupRepository.GetWithLinksAsync(request.GroupId, cancellationToken);

        if (group is null || group.TenantId != tenantContext.TenantId)
            throw new NotFoundException(nameof(group), request.GroupId);

        return mapper.Map<FooterLinkGroupDetailsDto>(group);
    }
}
