// ABOUTME: Query contract for the current tenant footer admin settings resource.
// ABOUTME: Returns scalar settings and lock states without loading footer link groups or links.

using Explore.Application.DTOs.Footer;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Queries;

public sealed record GetTenantFooterSettingsQuery : IRequest<TenantFooterSettingsDto>;
