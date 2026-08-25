// ABOUTME: Query to retrieve analytics governance settings for admin UI.

using Explore.Application.DTOs.Analytics;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public sealed record GetAnalyticsGovernanceSettingsQuery : IRequest<AnalyticsGovernanceSettingsDto>;
