using System.Net.Http.Json;

namespace Explore.Blazor.Client.Services;

public interface IMapsService
{
    Task<string> GetEmbedUrlAsync(string query);
}

public class MapsService : IMapsService
{
    private readonly HttpClient _httpClient;

    public MapsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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

            Console.WriteLine($"Error getting map embed URL: {response.StatusCode}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetEmbedUrlAsync: {ex.Message}");
            return string.Empty;
        }
    }
}
