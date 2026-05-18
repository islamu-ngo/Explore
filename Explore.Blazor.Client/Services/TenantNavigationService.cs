// ABOUTME: Service for managing tenant navigation links via HTTP calls.

using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Models.Responses;
using Explore.Blazor.Client.Services.Http;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Service for managing tenant navigation links.
/// Communicates with the API via HTTP to retrieve, create, update, delete, and reorder navigation links.
/// </summary>
public class TenantNavigationService : ITenantNavigationService
{
    private readonly HttpClient _httpClient;
    private readonly IApiClientExecutor _apiClientExecutor;
    private readonly ILogger<TenantNavigationService> _logger;
    private const string ApiEndpoint = "/api/tenant/navigation";

    public TenantNavigationService(
        HttpClient httpClient,
        ILogger<TenantNavigationService> logger,
        IApiClientExecutor? apiClientExecutor = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiClientExecutor = apiClientExecutor ?? new ApiClientExecutor();
    }

    /// <summary>
    /// Retrieves all navigation links for the current tenant.
    /// </summary>
    public async Task<ICollection<TenantNavigationLinkDto>> GetNavigationLinksAsync()
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<List<TenantNavigationLinkDto>>(
                ct => _httpClient.GetAsync(ApiEndpoint, ct),
                "tenant navigation links");

            if (!result.IsSuccess)
            {
                _logger.LogWarning("[TENANT NAVIGATION SERVICE] Failed to fetch navigation links: {StatusCode}", result.StatusCode);
                return new List<TenantNavigationLinkDto>();
            }

            return result.Value ?? new List<TenantNavigationLinkDto>();
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
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<Guid>>(
                ct => _httpClient.PostAsJsonAsync(ApiEndpoint, dto, ct),
                "tenant navigation create");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<Guid>(result, "[TENANT NAVIGATION SERVICE] Failed to create navigation link: {StatusCode}");
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
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<bool>>(
                ct => _httpClient.PutAsJsonAsync(url, dto, ct),
                "tenant navigation update");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<bool>(result, "[TENANT NAVIGATION SERVICE] Failed to update navigation link {Id}: {StatusCode}", id);
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
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<bool>>(
                ct => _httpClient.DeleteAsync(url, ct),
                "tenant navigation delete");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<bool>(result, "[TENANT NAVIGATION SERVICE] Failed to delete navigation link {Id}: {StatusCode}", id);
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
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<bool>>(
                ct => _httpClient.PutAsJsonAsync(url, orders, ct),
                "tenant navigation reorder");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<bool>(result, "[TENANT NAVIGATION SERVICE] Failed to reorder navigation links: {StatusCode}");
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

    private BaseCommandResponse<T> CreateFailureResponse<T>(ApiResult<BaseCommandResponse<T>> result, string logMessage, params object[] logArgs)
    {
        _logger.LogWarning(logMessage, [.. logArgs, result.StatusCode]);

        if (result.Problem is not null)
        {
            return new BaseCommandResponse<T>
            {
                Success = false,
                Message = $"API error: {result.StatusCode}",
                Errors = new List<string> { result.Problem.Title }
            };
        }

        var message = result.Exception?.Message ?? "Unknown error";
        return new BaseCommandResponse<T>
        {
            Success = false,
            Message = $"Error: {message}",
            Errors = new List<string> { message }
        };
    }
}
