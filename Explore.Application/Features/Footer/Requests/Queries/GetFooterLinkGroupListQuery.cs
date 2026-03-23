// ABOUTME: Query to list footer link groups for the current tenant (admin view).
// ABOUTME: Returns lightweight list DTOs without child links.

using Explore.Application.DTOs.Footer;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Queries;

public record GetFooterLinkGroupListQuery : IRequest<List<FooterLinkGroupListDto>>;
