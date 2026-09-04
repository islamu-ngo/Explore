// ABOUTME: Service contract for event moderation actions (light, heavy, unmoderate).
// ABOUTME: Extracted from monolithic EventService to enforce single responsibility.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services;

public interface IEventModerationService
{
    Task<BaseCommandResponseOfGuid?> ModerateEventLightAsync(Guid eventId, CancellationToken cancellationToken = default, string? reasonCode = null, string? correlationId = null);
    Task<BaseCommandResponseOfGuid?> ModerateEventHeavyAsync(Guid eventId, CancellationToken cancellationToken = default, string? reasonCode = null, string? correlationId = null);
    Task<BaseCommandResponseOfGuid?> UnmoderateEventAsync(Guid eventId, CancellationToken cancellationToken = default, string? reasonCode = null, string? correlationId = null);
}
