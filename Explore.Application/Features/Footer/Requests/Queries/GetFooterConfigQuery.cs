// ABOUTME: Query to retrieve the fully resolved footer configuration for the current tenant.
// ABOUTME: Used by the public Footer.razor component; returns settings + link groups.

using Explore.Application.DTOs.Footer;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Queries;

public record GetFooterConfigQuery : IRequest<FooterConfigDto>;
