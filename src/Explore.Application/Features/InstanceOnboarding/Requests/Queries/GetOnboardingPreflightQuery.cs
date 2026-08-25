// ABOUTME: Query contract for convention-first onboarding preflight checks.
// ABOUTME: Lets setup clients retrieve deterministic blocking and warning checks before launch.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public sealed record GetOnboardingPreflightQuery : IRequest<OnboardingPreflightDto>;
