// ABOUTME: Command contract for completing first-run instance onboarding.
// ABOUTME: Includes user identity data for auto-sync when user doesn't exist in the local database yet.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class CompleteInstanceOnboardingCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public required InstanceGovernanceSettingsDto Settings { get; set; } = new();

    /// <summary>
    /// User identity data extracted from authentication claims.
    /// Used to auto-create the user record during onboarding when the normal
    /// /api/User/sync endpoint cannot work (tenant doesn't exist yet).
    /// </summary>
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? AuthProvider { get; set; }
    public string? AuthProviderId { get; set; }
    public bool? EmailVerified { get; set; }
}
