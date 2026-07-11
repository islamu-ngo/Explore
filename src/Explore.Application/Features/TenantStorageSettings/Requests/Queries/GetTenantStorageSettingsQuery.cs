// ABOUTME: Query contract for reading current-tenant storage administration settings.
// ABOUTME: Returns effective policy, usage, lock state, and redacted optional S3 configuration.

using Explore.Application.DTOs.Tenant;
using MediatR;

namespace Explore.Application.Features.TenantStorageSettings.Requests.Queries;

public sealed class GetTenantStorageSettingsQuery : IRequest<TenantStorageSettingsDto>
{
}
