// ABOUTME: Query contract for retrieving tenant onboarding completion and current user eligibility.
// ABOUTME: Used by startup flow to route tenant administrators to policy onboarding.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Requests.Queries;

public sealed record GetTenantOnboardingStatusQuery : IRequest<TenantOnboardingStatusDto>
{
}
