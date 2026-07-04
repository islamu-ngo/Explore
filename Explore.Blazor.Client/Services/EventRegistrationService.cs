// ABOUTME: Blazor client service for event registration CRUD and management calls.
// ABOUTME: Wraps generated API calls and forwards optimistic concurrency stamps for PATCH updates.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Constants;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;

namespace Explore.Blazor.Client.Services;

public interface IEventRegistrationService
{
    Task<ICollection<EventRegistrationListDto>> GetAllRegistrationsAsync();
    Task<PaginatedResult<EventRegistrationListDto>> GetRegistrationsPagedAsync(int pageNumber, int pageSize);
    Task<EventRegistrationDto?> GetRegistrationByIdAsync(Guid registrationId);
    Task<BaseCommandResponseOfGuid?> RegisterForSessionAsync(CreateEventRegistrationDto dto);
    Task<BaseCommandResponseOfGuid?> UpdateRegistrationAsync(Guid id, Guid expectedConcurrencyStamp, UpdateEventRegistrationDto dto);
    Task<bool> CancelRegistrationAsync(Guid registrationId);
    Task<ICollection<EventRegistrationListDto>> GetRegistrationsBySessionAsync(Guid sessionId);
    Task<ICollection<EventRegistrationListDto>> GetRegistrationsByUserAsync(Guid userId);
    Task<bool> IsUserRegisteredForSessionAsync(Guid sessionId, Guid userId);
}

public class EventRegistrationService : IEventRegistrationService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventRegistrationService> _logger;

    public EventRegistrationService(IEventApiClient apiClient, ILogger<EventRegistrationService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ICollection<EventRegistrationListDto>> GetAllRegistrationsAsync()
    {
        try
        {
            _logger.LogInformation("[REGISTRATION SERVICE] Fetching all registrations...");
            var response = await _apiClient.GetEventRegistrationsAsync(ApiConstants.FirstPage, ApiConstants.DefaultPageSize);
            _logger.LogInformation("[REGISTRATION SERVICE] Received {Count} registrations from {Total} total", response?.Items?.Count ?? 0, response?.TotalCount ?? 0);
            return response?.Items ?? new List<EventRegistrationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] API error fetching registrations: {StatusCode}", ex.StatusCode);
            return new List<EventRegistrationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] Error fetching registrations");
            return new List<EventRegistrationListDto>();
        }
    }

    public async Task<PaginatedResult<EventRegistrationListDto>> GetRegistrationsPagedAsync(int pageNumber, int pageSize)
    {
        try
        {
            var response = await _apiClient.GetEventRegistrationsAsync(pageNumber, pageSize);
            return response.ToPaginatedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] Error fetching paged registrations (page {PageNumber}, size {PageSize})", pageNumber, pageSize);
            return PaginatedResult<EventRegistrationListDto>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<EventRegistrationDto?> GetRegistrationByIdAsync(Guid registrationId)
    {
        try
        {
            return await _apiClient.GetEventRegistrationByIdAsync(registrationId);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[REGISTRATION SERVICE] Registration not found: {RegistrationId}", registrationId);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] API error fetching registration {RegistrationId}: {StatusCode}", registrationId, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] Error fetching registration {RegistrationId}", registrationId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> RegisterForSessionAsync(CreateEventRegistrationDto dto)
    {
        try
        {
            _logger.LogInformation("[REGISTRATION SERVICE] Registering user for event: {EventId}", dto.EventId);
            return await _apiClient.CreateEventRegistrationAsync(body: dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] API error registering for session: {StatusCode}", ex.StatusCode);
            return CreateRegistrationFailureResponse(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] Error registering for session");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                FailureCode = "registration_failed",
                Message = "Registration failed. Please try again.",
                Errors = new List<string> { "Registration failed. Please try again." }
            };
        }
    }

    private static BaseCommandResponseOfGuid CreateRegistrationFailureResponse(ApiException ex)
    {
        var (failureCode, message) = ex.StatusCode switch
        {
            400 => ("registration_invalid", "Check the registration details and try again."),
            401 => ("registration_auth_required", "Sign in to register for this event."),
            403 => ("registration_forbidden", "You do not have permission to register for this event."),
            404 => ("registration_unavailable", "Registration is unavailable for this event."),
            409 => ("registration_conflict", "You are already registered or this registration changed. Refresh and try again."),
            429 => ("registration_rate_limited", "Too many registration attempts. Wait a moment and try again."),
            >= 500 => ("registration_temporarily_unavailable", "Registration is temporarily unavailable. Please try again later."),
            _ => ("registration_failed", "Registration failed. Please try again.")
        };

        return new BaseCommandResponseOfGuid
        {
            Success = false,
            FailureCode = failureCode,
            Message = message,
            Errors = new List<string> { message }
        };
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateRegistrationAsync(
        Guid id,
        Guid expectedConcurrencyStamp,
        UpdateEventRegistrationDto dto)
    {
        try
        {
            return await _apiClient.UpdateEventRegistrationAsync(
                id,
                dto,
                $"\"{expectedConcurrencyStamp:D}\"");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] API error updating registration: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] Error updating registration");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> CancelRegistrationAsync(Guid registrationId)
    {
        try
        {
            await _apiClient.DeleteEventRegistrationAsync(registrationId);
            _logger.LogInformation("[REGISTRATION SERVICE] Registration cancelled: {RegistrationId}", registrationId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] API error cancelling registration: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] Error cancelling registration");
            return false;
        }
    }

    public async Task<ICollection<EventRegistrationListDto>> GetRegistrationsBySessionAsync(Guid sessionId)
    {
        try
        {
            var response = await _apiClient.GetRegistrationsBySessionAsync(sessionId);
            return response ?? new List<EventRegistrationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] API error fetching session registrations: {StatusCode}", ex.StatusCode);
            return new List<EventRegistrationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] Error fetching session registrations");
            return new List<EventRegistrationListDto>();
        }
    }

    public async Task<ICollection<EventRegistrationListDto>> GetRegistrationsByUserAsync(Guid userId)
    {
        try
        {
            var response = await _apiClient.GetRegistrationsByUserAsync(userId);
            return response ?? new List<EventRegistrationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] API error fetching user registrations: {StatusCode}", ex.StatusCode);
            return new List<EventRegistrationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] Error fetching user registrations");
            return new List<EventRegistrationListDto>();
        }
    }

    public async Task<bool> IsUserRegisteredForSessionAsync(Guid sessionId, Guid userId)
    {
        try
        {
            var registrations = await GetRegistrationsByUserAsync(userId);
            return registrations.Any(r => r.EventSessionId == sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] Error checking user registration status");
            return false;
        }
    }
}
