// ABOUTME: Interface for EventAspect service wrapping IEventApiClient for Islamic and Tech aspects.
// Provides application-layer abstraction over generated API client methods.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Events;

/// <summary>
/// Service interface for managing Event Aspects (Islamic and Tech characteristics).
/// Wraps IEventApiClient methods with application-specific error handling.
/// </summary>
public interface IEventAspectService
{
    /// <summary>
    /// Gets the Islamic aspect for an event, if it exists.
    /// </summary>
    Task<EventIslamicAspectDto?> GetIslamicAspectAsync(Guid eventId);

    /// <summary>
    /// Gets the Tech aspect for an event, if it exists.
    /// </summary>
    Task<EventTechAspectDto?> GetTechAspectAsync(Guid eventId);

    /// <summary>
    /// Creates or updates the Islamic aspect for an event.
    /// </summary>
    Task<BaseCommandResponseOfGuid?> UpsertIslamicAspectAsync(Guid eventId, CreateUpdateIslamicAspectDto dto);

    /// <summary>
    /// Creates or updates the Tech aspect for an event.
    /// </summary>
    Task<BaseCommandResponseOfGuid?> UpsertTechAspectAsync(Guid eventId, CreateUpdateTechAspectDto dto);

    /// <summary>
    /// Deletes the Islamic aspect from an event.
    /// </summary>
    Task<bool> DeleteIslamicAspectAsync(Guid eventId);

    /// <summary>
    /// Deletes the Tech aspect from an event.
    /// </summary>
    Task<bool> DeleteTechAspectAsync(Guid eventId);
}
