// ABOUTME: Command contract for updating current-tenant storage override settings.
// ABOUTME: Carries tenant admin provider/quota/ceiling choices through the CQRS write pipeline.

using Explore.Application.DTOs.Tenant;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantStorageSettings.Requests.Commands;

public sealed class UpdateTenantStorageSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required TenantStorageSettingsDto Settings { get; set; } = new();
}
