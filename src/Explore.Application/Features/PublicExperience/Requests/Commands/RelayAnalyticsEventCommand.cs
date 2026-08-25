// ABOUTME: Relays browser-originated analytics events through the server when relay transport is enabled.
// ABOUTME: Used for anonymous-safe first-party transport that still respects tenant analytics governance.

using Explore.Application.DTOs.Analytics;
using MediatR;

namespace Explore.Application.Features.PublicExperience.Requests.Commands;

public sealed record RelayAnalyticsEventCommand : IRequest<bool>
{
    public Guid? AuthenticatedUserId { get; init; }
    public RelayAnalyticsEventDto Payload { get; init; } = new();
}
