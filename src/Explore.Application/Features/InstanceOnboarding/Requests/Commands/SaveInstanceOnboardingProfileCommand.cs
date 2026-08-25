// ABOUTME: CQRS command for saving the narrow non-secret instance onboarding profile during setup.
// ABOUTME: Carries only the existing profile DTO and never accepts route history, snapshots, or secret material.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public sealed record SaveInstanceOnboardingProfileCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required SelfHostOnboardingProfileDto Profile { get; init; }
}
