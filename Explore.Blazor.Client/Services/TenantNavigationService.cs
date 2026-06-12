// ABOUTME: Refit-backed service for managing tenant navigation links through BFF endpoints.
// ABOUTME: Provides safe fallback results while keeping navigation CRUD off raw HttpClient calls.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Models.Responses;
using Microsoft.Extensions.Logging;
using Refit;

namespace Explore.Blazor.Client.Services;

public interface ITenantNavigationApi
{
    [Get("/api/tenant/navigation")]
    Task<IApiResponse<List<TenantNavigationLinkDto>>> GetNavigationLinksAsync(CancellationToken cancellationToken);

    [Post("/api/tenant/navigation")]
    Task<IApiResponse<BaseCommandResponse<Guid>>> CreateNavigationLinkAsync(
        [Body] CreateTenantNavigationLinkDto dto,
        CancellationToken cancellationToken);

    [Put("/api/tenant/navigation/{id}")]
    Task<IApiResponse<BaseCommandResponse<bool>>> UpdateNavigationLinkAsync(
        Guid id,
        [Body] UpdateTenantNavigationLinkDto dto,
        CancellationToken cancellationToken);

    [Delete("/api/tenant/navigation/{id}")]
    Task<IApiResponse<BaseCommandResponse<bool>>> DeleteNavigationLinkAsync(
        Guid id,
        CancellationToken cancellationToken);

    [Put("/api/tenant/navigation/reorder")]
    Task<IApiResponse<BaseCommandResponse<bool>>> ReorderNavigationLinksAsync(
        [Body] List<UpdateTenantNavigationLinkOrderDto> orders,
        CancellationToken cancellationToken);
}

/// <summary>
/// Service for managing tenant navigation links.
/// Communicates with the API through BFF Refit endpoints to retrieve, create, update, delete, and reorder navigation links.
/// </summary>
public class TenantNavigationService : ITenantNavigationService
{
    private readonly ITenantNavigationApi _api;
    private readonly ILogger<TenantNavigationService> _logger;

    public TenantNavigationService(
        ITenantNavigationApi api,
        ILogger<TenantNavigationService> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all navigation links for the current tenant.
    /// </summary>
    public async Task<ICollection<TenantNavigationLinkDto>> GetNavigationLinksAsync()
    {
        try
        {
            var response = await _api.GetNavigationLinksAsync(CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TENANT NAVIGATION SERVICE] Failed to fetch navigation links: {StatusCode}", (int)response.StatusCode);
                return new List<TenantNavigationLinkDto>();
            }

            return response.Content ?? new List<TenantNavigationLinkDto>();
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
            var response = await _api.CreateNavigationLinkAsync(dto, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<Guid>(response, "[TENANT NAVIGATION SERVICE] Failed to create navigation link: {StatusCode}");
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
            var response = await _api.UpdateNavigationLinkAsync(id, dto, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<bool>(response, "[TENANT NAVIGATION SERVICE] Failed to update navigation link {Id}: {StatusCode}", id);
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
            var response = await _api.DeleteNavigationLinkAsync(id, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<bool>(response, "[TENANT NAVIGATION SERVICE] Failed to delete navigation link {Id}: {StatusCode}", id);
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
            var response = await _api.ReorderNavigationLinksAsync(orders, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<bool>(response, "[TENANT NAVIGATION SERVICE] Failed to reorder navigation links: {StatusCode}");
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

    private BaseCommandResponse<T> CreateFailureResponse<T>(IApiResponse response, string logMessage, params object[] logArgs)
    {
        _logger.LogWarning(logMessage, [.. logArgs, (int)response.StatusCode]);
        var message = response.Error?.Content ?? response.Error?.Message ?? "Unknown error";
        return new BaseCommandResponse<T>
        {
            Success = false,
            Message = $"Error: {message}",
            Errors = new List<string> { message }
        };
    }
}
