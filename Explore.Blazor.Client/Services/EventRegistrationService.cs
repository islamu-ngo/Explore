using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Constants;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public interface IEventRegistrationService
{
    Task<ICollection<EventRegistrationListDto>> GetAllRegistrationsAsync();
    Task<PaginatedResult<EventRegistrationListDto>> GetRegistrationsPagedAsync(int pageNumber, int pageSize);
    Task<EventRegistrationDto?> GetRegistrationByIdAsync(Guid registrationId);
    Task<BaseCommandResponseOfGuid?> RegisterForSessionAsync(CreateEventRegistrationDto dto);
    Task<BaseCommandResponseOfGuid?> UpdateRegistrationAsync(Guid id, UpdateEventRegistrationDto dto);
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
            return await _apiClient.CreateEventRegistrationAsync(dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] API error registering for session: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] Error registering for session");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateRegistrationAsync(Guid id, UpdateEventRegistrationDto dto)
    {
        try
        {
            return await _apiClient.UpdateEventRegistrationAsync(id, dto);
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
            var registrations = await GetRegistrationsBySessionAsync(sessionId);
            return registrations.Any(r => r.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REGISTRATION SERVICE] Error checking user registration status");
            return false;
        }
    }
}

