// ABOUTME: Command request for reconciling instance-wide storage usage counters.
// ABOUTME: Rebuilds used/quarantined object totals from metadata while preserving active reservations.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public sealed class RecalculateInstanceStorageUsageCommand : IRequest<InstanceStorageUsageDto>
{
}
