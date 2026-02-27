// ABOUTME: Service for managing Event Aspects (Islamic and Tech) using the generated IEventApiClient.
// Wraps NSwag-generated client methods with application-specific error handling.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Service implementation for managing Event Aspects (Islamic and Tech characteristics).
/// Uses the NSwag-generated IEventApiClient for type-safe API communication.
/// </summary>
public class EventAspectService : IEventAspectService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventAspectService> _logger;

    public EventAspectService(IEventApiClient apiClient, ILogger<EventAspectService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EventIslamicAspectDto?> GetIslamicAspectAsync(Guid eventId)
    {
        try
        {
            return await _apiClient.GetEventIslamicAspectAsync(eventId);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogDebug("Islamic aspect not found for event {EventId}", eventId);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error fetching Islamic aspect for event {EventId}. Status: {StatusCode}",
                eventId, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Islamic aspect for event {EventId}", eventId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<EventTechAspectDto?> GetTechAspectAsync(Guid eventId)
    {
        try
        {
            return await _apiClient.GetEventTechAspectAsync(eventId);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogDebug("Tech aspect not found for event {EventId}", eventId);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error fetching Tech aspect for event {EventId}. Status: {StatusCode}",
                eventId, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Tech aspect for event {EventId}", eventId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<BaseCommandResponseOfGuid?> UpsertIslamicAspectAsync(Guid eventId, CreateUpdateIslamicAspectDto dto)
    {
        try
        {
            return await _apiClient.UpsertEventIslamicAspectAsync(eventId, dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error upserting Islamic aspect for event {EventId}. Status: {StatusCode}",
                eventId, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting Islamic aspect for event {EventId}", eventId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<BaseCommandResponseOfGuid?> UpsertTechAspectAsync(Guid eventId, CreateUpdateTechAspectDto dto)
    {
        try
        {
            return await _apiClient.UpsertEventTechAspectAsync(eventId, dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error upserting Tech aspect for event {EventId}. Status: {StatusCode}",
                eventId, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting Tech aspect for event {EventId}", eventId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteIslamicAspectAsync(Guid eventId)
    {
        try
        {
            await _apiClient.DeleteEventIslamicAspectAsync(eventId);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogDebug("Islamic aspect already deleted or not found for event {EventId}", eventId);
            return true; // Consider not found as successful deletion
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error deleting Islamic aspect for event {EventId}. Status: {StatusCode}",
                eventId, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Islamic aspect for event {EventId}", eventId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTechAspectAsync(Guid eventId)
    {
        try
        {
            await _apiClient.DeleteEventTechAspectAsync(eventId);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogDebug("Tech aspect already deleted or not found for event {EventId}", eventId);
            return true; // Consider not found as successful deletion
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error deleting Tech aspect for event {EventId}. Status: {StatusCode}",
                eventId, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Tech aspect for event {EventId}", eventId);
            return false;
        }
    }
}
