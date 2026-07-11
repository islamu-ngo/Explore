// ABOUTME: Query to get a single footer link group with all child links (admin edit view).
// ABOUTME: Throws NotFoundException if the group does not belong to the current tenant.

using Explore.Application.DTOs.Footer;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Queries;

public record GetFooterLinkGroupDetailsQuery(Guid GroupId) : IRequest<FooterLinkGroupDetailsDto>;
