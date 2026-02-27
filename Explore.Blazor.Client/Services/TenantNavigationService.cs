// ABOUTME: Service for managing tenant navigation links via HTTP calls.

using System.Net.Http.Json;
using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Service for managing tenant navigation links.
/// Communicates with the API via HTTP to retrieve, create, update, delete, and reorder navigation links.
/// </summary>
public class TenantNavigationService : ITenantNavigationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TenantNavigationService> _logger;
    private const string ApiEndpoint = "/api/tenant/navigation";

    public TenantNavigationService(HttpClient httpClient, ILogger<TenantNavigationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all navigation links for the current tenant.
    /// </summary>
    public async Task<ICollection<TenantNavigationLinkDto>> GetNavigationLinksAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(ApiEndpoint);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TENANT NAVIGATION SERVICE] Failed to fetch navigation links: {StatusCode}", response.StatusCode);
                return new List<TenantNavigationLinkDto>();
            }

            var links = await response.Content.ReadFromJsonAsync<List<TenantNavigationLinkDto>>();
            return links ?? new List<TenantNavigationLinkDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TENANT NAVIGATION SERVICE] Error fetching navigation links");
            return new List<TenantNavigationLinkDto>();
        }
    }

    /// <summary>
    /// Creates a new navigation link for the tenant.
    /// </summary>
    public async Task<BaseCommandResponse<Guid>?> CreateNavigationLinkAsync(CreateTenantNavigationLinkDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint, dto);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TENANT NAVIGATION SERVICE] Failed to create navigation link: {StatusCode}", response.StatusCode);
                return new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            var result = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TENANT NAVIGATION SERVICE] Error creating navigation link");
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Updates an existing navigation link.
    /// </summary>
    public async Task<BaseCommandResponse<bool>?> UpdateNavigationLinkAsync(Guid id, UpdateTenantNavigationLinkDto dto)
    {
        try
        {
            var url = $"{ApiEndpoint}/{id}";
            var response = await _httpClient.PutAsJsonAsync(url, dto);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TENANT NAVIGATION SERVICE] Failed to update navigation link {Id}: {StatusCode}", id, response.StatusCode);
                return new BaseCommandResponse<bool>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            var result = await response.Content.ReadFromJsonAsync<BaseCommandResponse<bool>>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TENANT NAVIGATION SERVICE] Error updating navigation link {Id}", id);
            return new BaseCommandResponse<bool>
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Deletes a navigation link.
    /// </summary>
    public async Task<BaseCommandResponse<bool>?> DeleteNavigationLinkAsync(Guid id)
    {
        try
        {
            var url = $"{ApiEndpoint}/{id}";
            var response = await _httpClient.DeleteAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TENANT NAVIGATION SERVICE] Failed to delete navigation link {Id}: {StatusCode}", id, response.StatusCode);
                return new BaseCommandResponse<bool>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            var result = await response.Content.ReadFromJsonAsync<BaseCommandResponse<bool>>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TENANT NAVIGATION SERVICE] Error deleting navigation link {Id}", id);
            return new BaseCommandResponse<bool>
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Reorders multiple navigation links.
    /// </summary>
    public async Task<BaseCommandResponse<bool>?> ReorderNavigationLinksAsync(List<UpdateTenantNavigationLinkOrderDto> orders)
    {
        try
        {
            var url = $"{ApiEndpoint}/reorder";
            var response = await _httpClient.PutAsJsonAsync(url, orders);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TENANT NAVIGATION SERVICE] Failed to reorder navigation links: {StatusCode}", response.StatusCode);
                return new BaseCommandResponse<bool>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            var result = await response.Content.ReadFromJsonAsync<BaseCommandResponse<bool>>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TENANT NAVIGATION SERVICE] Error reordering navigation links");
            return new BaseCommandResponse<bool>
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
