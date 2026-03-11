// ABOUTME: Command to update analytics governance settings from admin UI.

using Explore.Application.DTOs.Analytics;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public sealed class UpdateAnalyticsGovernanceSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required Guid UserId { get; init; }
    public required AnalyticsGovernanceSettingsDto Settings { get; init; }
}
