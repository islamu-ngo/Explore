// ABOUTME: Relays browser-originated analytics events through the server when relay transport is enabled.
// ABOUTME: Used for anonymous-safe first-party transport that still respects tenant analytics governance.

using Explore.Application.DTOs.Analytics;
using MediatR;

namespace Explore.Application.Features.PublicExperience.Requests.Commands;

public class RelayAnalyticsEventCommand : IRequest<bool>
{
    public Guid? AuthenticatedUserId { get; set; }
    public RelayAnalyticsEventDto Payload { get; set; } = new();
}
