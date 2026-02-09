// ABOUTME: Query contract for reading effective instance governance settings.
// ABOUTME: Used by onboarding and instance admin settings pages.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public class GetInstanceGovernanceSettingsQuery : IRequest<InstanceGovernanceSettingsDto>
{
}
