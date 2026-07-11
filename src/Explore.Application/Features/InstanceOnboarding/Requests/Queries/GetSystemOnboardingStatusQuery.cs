// ABOUTME: Query contract for public, non-sensitive system onboarding state.
// ABOUTME: Lets UI hosts discover first-run mode without reading deployment secrets directly.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public sealed class GetSystemOnboardingStatusQuery : IRequest<SystemOnboardingStatusDto>;
