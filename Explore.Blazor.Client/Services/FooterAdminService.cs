// ABOUTME: HTTP service for managing footer link groups, links, and tenant settings.
// ABOUTME: Follows TenantNavigationService pattern — typed HttpClient with resilience.

using System.Net.Http.Json;
using Explore.Blazor.Client.Contracts.Services.Footer;
using Explore.Blazor.Client.Models.Responses;
using Explore.Blazor.Client.Services.Http;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Communicates with FooterController endpoints to manage link groups, links, and tenant footer settings.
/// </summary>
public class FooterAdminService : IFooterAdminService
{
    private readonly HttpClient _httpClient;
    private readonly IApiClientExecutor _apiClientExecutor;
    private readonly ILogger<FooterAdminService> _logger;
    private const string LinkGroupsEndpoint = "/api/footer/link-groups";
    private const string LinksEndpoint = "/api/footer/links";
    private const string SettingsEndpoint = "/api/footer/settings";
    private const string ConfigEndpoint = "/api/footer/config";

    public FooterAdminService(
        HttpClient httpClient,
        ILogger<FooterAdminService> logger,
        IApiClientExecutor? apiClientExecutor = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiClientExecutor = apiClientExecutor ?? new ApiClientExecutor();
    }

    // ── Link Groups ──────────────────────────────────────────────────────

    public async Task<List<FooterLinkGroupListModel>> GetLinkGroupsAsync()
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<List<FooterLinkGroupListModel>>(
                ct => _httpClient.GetAsync(LinkGroupsEndpoint, ct),
                "footer link groups");

            if (!result.IsSuccess)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to fetch link groups: {StatusCode}", result.StatusCode);
                return new List<FooterLinkGroupListModel>();
            }

            return result.Value ?? new List<FooterLinkGroupListModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOOTER ADMIN] Error fetching link groups");
            return new List<FooterLinkGroupListModel>();
        }
    }

    public async Task<FooterLinkGroupDetailsModel?> GetLinkGroupAsync(Guid id)
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<FooterLinkGroupDetailsModel>(
                ct => _httpClient.GetAsync($"{LinkGroupsEndpoint}/{id}", ct),
                "footer link group");

            if (!result.IsSuccess)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to fetch link group {Id}: {StatusCode}", id, result.StatusCode);
                return null;
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOOTER ADMIN] Error fetching link group {Id}", id);
            return null;
        }
    }

    public async Task<BaseCommandResponse<Guid>?> CreateLinkGroupAsync(CreateFooterLinkGroupModel model)
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<Guid>>(
                ct => _httpClient.PostAsJsonAsync(LinkGroupsEndpoint, model, ct),
                "footer link group create");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<Guid>(result, "[FOOTER ADMIN] Failed to create link group: {StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOOTER ADMIN] Error creating link group");
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BaseCommandResponse<Guid>?> UpdateLinkGroupAsync(Guid id, UpdateFooterLinkGroupModel model)
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<Guid>>(
                ct => _httpClient.PutAsJsonAsync($"{LinkGroupsEndpoint}/{id}", model, ct),
                "footer link group update");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<Guid>(result, "[FOOTER ADMIN] Failed to update link group {Id}: {StatusCode}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOOTER ADMIN] Error updating link group {Id}", id);
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BaseCommandResponse<bool>?> DeleteLinkGroupAsync(Guid id)
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<bool>>(
                ct => _httpClient.DeleteAsync($"{LinkGroupsEndpoint}/{id}", ct),
                "footer link group delete");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<bool>(result, "[FOOTER ADMIN] Failed to delete link group {Id}: {StatusCode}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOOTER ADMIN] Error deleting link group {Id}", id);
            return new BaseCommandResponse<bool>
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BaseCommandResponse<Guid>?> ReorderLinkGroupsAsync(List<Guid> orderedIds)
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<Guid>>(
                ct => _httpClient.PostAsJsonAsync($"{LinkGroupsEndpoint}/reorder", orderedIds, ct),
                "footer link group reorder");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<Guid>(result, "[FOOTER ADMIN] Failed to reorder link groups: {StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOOTER ADMIN] Error reordering link groups");
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    // ── Links ────────────────────────────────────────────────────────────

    public async Task<BaseCommandResponse<Guid>?> CreateLinkAsync(Guid groupId, CreateFooterLinkModel model)
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<Guid>>(
                ct => _httpClient.PostAsJsonAsync($"{LinkGroupsEndpoint}/{groupId}/links", model, ct),
                "footer link create");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<Guid>(result, "[FOOTER ADMIN] Failed to create link in group {GroupId}: {StatusCode}", groupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOOTER ADMIN] Error creating link in group {GroupId}", groupId);
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BaseCommandResponse<Guid>?> UpdateLinkAsync(Guid id, UpdateFooterLinkModel model)
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<Guid>>(
                ct => _httpClient.PutAsJsonAsync($"{LinksEndpoint}/{id}", model, ct),
                "footer link update");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<Guid>(result, "[FOOTER ADMIN] Failed to update link {Id}: {StatusCode}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOOTER ADMIN] Error updating link {Id}", id);
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BaseCommandResponse<bool>?> DeleteLinkAsync(Guid id)
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<bool>>(
                ct => _httpClient.DeleteAsync($"{LinksEndpoint}/{id}", ct),
                "footer link delete");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<bool>(result, "[FOOTER ADMIN] Failed to delete link {Id}: {StatusCode}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOOTER ADMIN] Error deleting link {Id}", id);
            return new BaseCommandResponse<bool>
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    // ── Footer Settings (read) ───────────────────────────────────────────

    public async Task<FooterSettingsResponseModel?> GetFooterSettingsAsync()
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<FooterConfigEnvelope>(
                ct => _httpClient.GetAsync(ConfigEndpoint, ct),
                "footer settings");

            if (!result.IsSuccess)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to fetch footer config: {StatusCode}", result.StatusCode);
                return null;
            }

            return result.Value?.Settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOOTER ADMIN] Error fetching footer settings");
            return null;
        }
    }

    // ── Tenant Footer Settings (write) ───────────────────────────────────

    public async Task<BaseCommandResponse<Guid>?> UpdateTenantSettingsAsync(UpdateTenantFooterSettingsModel model)
    {
        try
        {
            var result = await _apiClientExecutor.ReadJsonAsync<BaseCommandResponse<Guid>>(
                ct => _httpClient.PutAsJsonAsync(SettingsEndpoint, model, ct),
                "footer settings update");

            return result.IsSuccess
                ? result.Value
                : CreateFailureResponse<Guid>(result, "[FOOTER ADMIN] Failed to update tenant footer settings: {StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOOTER ADMIN] Error updating tenant footer settings");
            return new BaseCommandResponse<Guid>
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

    /// <summary>
    /// Wrapper to deserialize the config endpoint response and extract the settings portion.
    /// </summary>
    private class FooterConfigEnvelope
    {
        public FooterSettingsResponseModel? Settings { get; set; }
    }
}
