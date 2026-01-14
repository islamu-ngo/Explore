using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public interface ILocationService
{
    Task<ICollection<LocationListDto>> GetAllLocationsAsync();
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
            _logger.LogInformation("[LOCATION SERVICE] Fetching all locations...");
            var response = await _apiClient.LocationAllAsync();
            _logger.LogInformation("[LOCATION SERVICE] Received {Count} locations", response?.Count ?? 0);
            return response ?? new List<LocationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error fetching locations: {StatusCode}", ex.StatusCode);
            return new List<LocationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] Error fetching locations");
            return new List<LocationListDto>();
        }
    }

    public async Task<LocationDto?> GetLocationByIdAsync(Guid locationId)
    {
        try
        {
            return await _apiClient.LocationGETAsync(locationId);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] Error fetching location {LocationId}", locationId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateLocationAsync(CreateLocationDto dto)
    {
        try
        {
            _logger.LogInformation("[LOCATION SERVICE] Creating location: {Name}", dto.FullName);
            return await _apiClient.LocationPOSTAsync(dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error creating location: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] Error creating location");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateLocationAsync(Guid id, UpdateLocationDto dto)
    {
        try
        {
            return await _apiClient.LocationPUTAsync(id, dto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error updating location: {StatusCode}", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] Error updating location");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> DeleteLocationAsync(Guid locationId)
    {
        try
        {
            await _apiClient.LocationDELETEAsync(locationId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error deleting location: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] Error deleting location");
            return false;
        }
    }

    public async Task<ICollection<LocationListDto>> GetLocationsByCityAsync(string city)
    {
        try
        {
            var response = await _apiClient.ByCityAsync(city);
            return response ?? new List<LocationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error fetching locations by city: {StatusCode}", ex.StatusCode);
            return new List<LocationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] Error fetching locations by city");
            return new List<LocationListDto>();
        }
    }

    public async Task<ICollection<LocationListDto>> GetLocationsByCountryAsync(string country)
    {
        try
        {
            var response = await _apiClient.ByCountryAsync(country);
            return response ?? new List<LocationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] API error fetching locations by country: {StatusCode}", ex.StatusCode);
            return new List<LocationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCATION SERVICE] Error fetching locations by country");
            return new List<LocationListDto>();
        }
    }
}
