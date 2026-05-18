// ABOUTME: Query contract for reading the current tenant branding typed settings document.
// ABOUTME: Uses typed settings documents directly without scalar fallback or dual writes.

using Explore.Application.DTOs.TenantSettingsDocuments;
using MediatR;

namespace Explore.Application.Features.TenantSettingsDocuments.Requests.Queries;

public sealed record GetTenantBrandingSettingsDocumentQuery : IRequest<TenantBrandingSettingsDocumentDto?>;
