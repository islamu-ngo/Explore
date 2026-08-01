// ABOUTME: Typed client boundary for Studio navigation context and registration-order reads.
// ABOUTME: Keeps Studio components behind generated API contracts and server-authored HAL affordances.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services;

public interface IStudioContextService
{
    Task<HalResourceOfStudioContextDto?> GetContextAsync(Guid? actorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HalResourceOfRegistrationOrderDto>> GetEventOrdersAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudioAttendeeOrder>> GetEventAttendeesAsync(Guid eventId, CancellationToken cancellationToken = default);
}
