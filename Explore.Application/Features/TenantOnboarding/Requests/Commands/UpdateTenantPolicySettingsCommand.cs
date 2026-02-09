// ABOUTME: Command contract for runtime updates to tenant policy settings.
// ABOUTME: Allows tenant administrators or instance administrators to modify tenant policies.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Requests.Commands;

public class UpdateTenantPolicySettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required TenantPolicySettingsDto Settings { get; set; } = new();
}
