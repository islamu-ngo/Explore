// ABOUTME: Query contract for retrieving first-run onboarding completion and current user scope state.
// ABOUTME: Used by startup routing to determine onboarding or normal application entry.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public class GetInstanceOnboardingStatusQuery : IRequest<InstanceOnboardingStatusDto>
{
}
