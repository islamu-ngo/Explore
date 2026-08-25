// ABOUTME: Query contract for reading effective tenant onboarding policy settings.
// ABOUTME: Supports tenant onboarding questionnaire defaults and runtime policy editing screens.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Requests.Queries;

public sealed record GetTenantPolicySettingsQuery : IRequest<TenantPolicySettingsDto>
{
}
