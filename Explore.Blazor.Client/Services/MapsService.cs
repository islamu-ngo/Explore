using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Service for maps-related operations.
/// </summary>
public interface IMapsService
{
    Task<string> GetEmbedUrlAsync(string query);
}

/// <summary>
/// Implementation of maps service.
/// </summary>
public class MapsService : IMapsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MapsService> _logger;

    public MapsService(HttpClient httpClient, ILogger<MapsService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetEmbedUrlAsync(string query)
    {
        try
        {
            if (string.IsNullOrEmpty(query))
            {
                return string.Empty;
            }

            var encodedQuery = Uri.EscapeDataString(query);
            var response = await _httpClient.GetAsync($"/bff/api/Maps/embed-url?query={encodedQuery}");

            if (response.IsSuccessStatusCode)
            {
                var embedUrl = await response.Content.ReadAsStringAsync();
                // Remove quotes from JSON response
                return embedUrl?.Trim('"') ?? string.Empty;
            }

            _logger.LogWarning("Error getting map embed URL: {StatusCode}", response.StatusCode);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetEmbedUrlAsync");
            return string.Empty;
        }
    }
}
