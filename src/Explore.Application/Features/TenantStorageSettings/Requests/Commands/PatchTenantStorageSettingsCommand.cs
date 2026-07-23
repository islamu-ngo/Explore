// ABOUTME: Command contract for patching current-tenant storage override settings.
// ABOUTME: Carries presence-aware policy and S3 groups through the CQRS write pipeline.

using Explore.Application.DTOs.Tenant;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantStorageSettings.Requests.Commands;

public sealed class PatchTenantStorageSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required PatchTenantStorageSettingsDto Settings { get; set; } = new();
}
