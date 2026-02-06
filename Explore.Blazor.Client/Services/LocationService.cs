// ABOUTME: Service for managing location-related operations.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Constants;
using Explore.Blazor.Client.Helpers;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public interface ILocationService
{
    Task<ICollection<LocationListDto>> GetAllLocationsAsync();
    Task<ICollection<LocationListDto>> GetLocations(); // Alias for admin pages
    Task<LocationDto?> GetLocationByIdAsync(Guid locationId);
    Task<BaseCommandResponseOfGuid?> CreateLocationAsync(CreateLocationDto dto);
    Task<BaseCommandResponseOfGuid?> UpdateLocationAsync(Guid id, UpdateLocationDto dto);
    Task<bool> DeleteLocationAsync(Guid locationId);
    Task<ICollection<LocationListDto>> GetLocationsByCityAsync(string city);
    Task<ICollection<LocationListDto>> GetLocationsByCountryAsync(string country);
}

public class LocationService : ILocationService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<LocationService> _logger;

    public LocationService(IEventApiClient apiClient, ILogger<LocationService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ICollection<LocationListDto>> GetAllLocationsAsync()
    {
        try
        {
            var result = await _apiClient.GetLocationsAsync(pageNumber: ApiConstants.FirstPage, pageSize: ApiConstants.DefaultPageSize);
            return result?.GetItems() ?? new List<LocationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error fetching locations: {StatusCode}", ex.StatusCode);
            return new List<LocationListDto>();
        }
    }

    public Task<ICollection<LocationListDto>> GetLocations() => GetAllLocationsAsync();

    public async Task<LocationDto?> GetLocationByIdAsync(Guid locationId)
    {
        try
        {
            var result = await _apiClient.GetLocationByIdAsync(locationId);
            return result?.ToDto();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[LOCATION SERVICE] Location not found: {LocationId}", locationId);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error fetching location {LocationId}: {StatusCode}", locationId, ex.StatusCode);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateLocationAsync(CreateLocationDto dto)
    {
        try
        {
            return await _apiClient.CreateLocationAsync(dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error creating location: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid { Success = false, Message = $"API error: {ex.Message}", Errors = new List<string> { ex.Response ?? ex.Message } };
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateLocationAsync(Guid id, UpdateLocationDto dto)
    {
        try
        {
            return await _apiClient.UpdateLocationAsync(id, dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error updating location: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid { Success = false, Message = $"API error: {ex.Message}", Errors = new List<string> { ex.Response ?? ex.Message } };
        }
    }

    public async Task<bool> DeleteLocationAsync(Guid locationId)
    {
        try
        {
            await _apiClient.DeleteLocationAsync(locationId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error deleting location: {StatusCode}", ex.StatusCode);
            return false;
        }
    }

    public async Task<ICollection<LocationListDto>> GetLocationsByCityAsync(string city)
    {
        try
        {
            var result = await _apiClient.GetLocationsByCityAsync(city);
            return result?.GetItems() ?? new List<LocationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error fetching locations by city: {StatusCode}", ex.StatusCode);
            return new List<LocationListDto>();
        }
    }

    public async Task<ICollection<LocationListDto>> GetLocationsByCountryAsync(string country)
    {
        try
        {
            var result = await _apiClient.GetLocationsByCountryAsync(country);
            return result?.GetItems() ?? new List<LocationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error fetching locations by country: {StatusCode}", ex.StatusCode);
            return new List<LocationListDto>();
        }
    }
}
