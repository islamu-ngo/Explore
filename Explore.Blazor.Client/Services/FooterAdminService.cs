// ABOUTME: HTTP service for managing footer link groups, links, and tenant settings.
// ABOUTME: Follows TenantNavigationService pattern — typed HttpClient with resilience.

using System.Net.Http.Json;
using Explore.Blazor.Client.Contracts.Services.Footer;
using Explore.Blazor.Client.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Communicates with FooterController endpoints to manage link groups, links, and tenant footer settings.
/// </summary>
public class FooterAdminService : IFooterAdminService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FooterAdminService> _logger;
    private const string LinkGroupsEndpoint = "/api/footer/link-groups";
    private const string LinksEndpoint = "/api/footer/links";
    private const string SettingsEndpoint = "/api/footer/settings";
    private const string ConfigEndpoint = "/api/footer/config";

    public FooterAdminService(HttpClient httpClient, ILogger<FooterAdminService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Link Groups ──────────────────────────────────────────────────────

    public async Task<List<FooterLinkGroupListModel>> GetLinkGroupsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(LinkGroupsEndpoint);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to fetch link groups: {StatusCode}", response.StatusCode);
                return new List<FooterLinkGroupListModel>();
            }

            var groups = await response.Content.ReadFromJsonAsync<List<FooterLinkGroupListModel>>();
            return groups ?? new List<FooterLinkGroupListModel>();
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
            var response = await _httpClient.GetAsync($"{LinkGroupsEndpoint}/{id}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to fetch link group {Id}: {StatusCode}", id, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<FooterLinkGroupDetailsModel>();
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
            var response = await _httpClient.PostAsJsonAsync(LinkGroupsEndpoint, model);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to create link group: {StatusCode}", response.StatusCode);
                return new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            return await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
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
            var response = await _httpClient.PutAsJsonAsync($"{LinkGroupsEndpoint}/{id}", model);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to update link group {Id}: {StatusCode}", id, response.StatusCode);
                return new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            return await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
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
            var response = await _httpClient.DeleteAsync($"{LinkGroupsEndpoint}/{id}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to delete link group {Id}: {StatusCode}", id, response.StatusCode);
                return new BaseCommandResponse<bool>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            return await response.Content.ReadFromJsonAsync<BaseCommandResponse<bool>>();
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
            var response = await _httpClient.PostAsJsonAsync($"{LinkGroupsEndpoint}/reorder", orderedIds);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to reorder link groups: {StatusCode}", response.StatusCode);
                return new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            return await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
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
            var response = await _httpClient.PostAsJsonAsync($"{LinkGroupsEndpoint}/{groupId}/links", model);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to create link in group {GroupId}: {StatusCode}", groupId, response.StatusCode);
                return new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            return await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
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
            var response = await _httpClient.PutAsJsonAsync($"{LinksEndpoint}/{id}", model);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to update link {Id}: {StatusCode}", id, response.StatusCode);
                return new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            return await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
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
            var response = await _httpClient.DeleteAsync($"{LinksEndpoint}/{id}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to delete link {Id}: {StatusCode}", id, response.StatusCode);
                return new BaseCommandResponse<bool>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            return await response.Content.ReadFromJsonAsync<BaseCommandResponse<bool>>();
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
            var response = await _httpClient.GetAsync(ConfigEndpoint);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to fetch footer config: {StatusCode}", response.StatusCode);
                return null;
            }

            var config = await response.Content.ReadFromJsonAsync<FooterConfigEnvelope>();
            return config?.Settings;
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
            var response = await _httpClient.PutAsJsonAsync(SettingsEndpoint, model);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to update tenant footer settings: {StatusCode}", response.StatusCode);
                return new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }

            return await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
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

    /// <summary>
    /// Wrapper to deserialize the config endpoint response and extract the settings portion.
    /// </summary>
    private class FooterConfigEnvelope
    {
        public FooterSettingsResponseModel? Settings { get; set; }
    }
}
