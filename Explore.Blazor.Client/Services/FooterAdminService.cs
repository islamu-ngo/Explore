// ABOUTME: Refit-backed service for managing footer link groups, links, and tenant settings.
// ABOUTME: Keeps footer governance calls on the shared BFF transport without raw HttpClient calls.

using Explore.Blazor.Client.Contracts.Services.Footer;
using Explore.Blazor.Client.Models.Responses;
using Microsoft.Extensions.Logging;
using Refit;

namespace Explore.Blazor.Client.Services;

public sealed class FooterConfigEnvelope
{
    public FooterSettingsResponseModel? Settings { get; set; }
}

public interface IFooterAdminApi
{
    [Get("/api/footer/link-groups")]
    Task<IApiResponse<List<FooterLinkGroupListModel>>> GetLinkGroupsAsync(CancellationToken cancellationToken);

    [Get("/api/footer/link-groups/{id}")]
    Task<IApiResponse<FooterLinkGroupDetailsModel>> GetLinkGroupAsync(Guid id, CancellationToken cancellationToken);

    [Post("/api/footer/link-groups")]
    Task<IApiResponse<BaseCommandResponse<Guid>>> CreateLinkGroupAsync(
        [Body] CreateFooterLinkGroupModel model,
        CancellationToken cancellationToken);

    [Put("/api/footer/link-groups/{id}")]
    Task<IApiResponse<BaseCommandResponse<Guid>>> UpdateLinkGroupAsync(
        Guid id,
        [Body] UpdateFooterLinkGroupModel model,
        CancellationToken cancellationToken);

    [Delete("/api/footer/link-groups/{id}")]
    Task<IApiResponse<BaseCommandResponse<bool>>> DeleteLinkGroupAsync(Guid id, CancellationToken cancellationToken);

    [Post("/api/footer/link-groups/reorder")]
    Task<IApiResponse<BaseCommandResponse<Guid>>> ReorderLinkGroupsAsync(
        [Body] List<Guid> orderedIds,
        CancellationToken cancellationToken);

    [Post("/api/footer/link-groups/{groupId}/links")]
    Task<IApiResponse<BaseCommandResponse<Guid>>> CreateLinkAsync(
        Guid groupId,
        [Body] CreateFooterLinkModel model,
        CancellationToken cancellationToken);

    [Put("/api/footer/links/{id}")]
    Task<IApiResponse<BaseCommandResponse<Guid>>> UpdateLinkAsync(
        Guid id,
        [Body] UpdateFooterLinkModel model,
        CancellationToken cancellationToken);

    [Delete("/api/footer/links/{id}")]
    Task<IApiResponse<BaseCommandResponse<bool>>> DeleteLinkAsync(Guid id, CancellationToken cancellationToken);

    [Get("/api/footer/config")]
    Task<IApiResponse<FooterConfigEnvelope>> GetFooterConfigAsync(CancellationToken cancellationToken);

    [Put("/api/footer/settings")]
    Task<IApiResponse<BaseCommandResponse<Guid>>> UpdateTenantSettingsAsync(
        [Body] UpdateTenantFooterSettingsModel model,
        CancellationToken cancellationToken);
}

/// <summary>
/// Communicates with FooterController endpoints to manage link groups, links, and tenant footer settings.
/// </summary>
public class FooterAdminService : IFooterAdminService
{
    private readonly IFooterAdminApi _api;
    private readonly ILogger<FooterAdminService> _logger;

    public FooterAdminService(
        IFooterAdminApi api,
        ILogger<FooterAdminService> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Link Groups ──────────────────────────────────────────────────────

    public async Task<List<FooterLinkGroupListModel>> GetLinkGroupsAsync()
    {
        try
        {
            var response = await _api.GetLinkGroupsAsync(CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to fetch link groups: {StatusCode}", (int)response.StatusCode);
                return new List<FooterLinkGroupListModel>();
            }

            return response.Content ?? new List<FooterLinkGroupListModel>();
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
            var response = await _api.GetLinkGroupAsync(id, CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to fetch link group {Id}: {StatusCode}", id, (int)response.StatusCode);
                return null;
            }

            return response.Content;
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
            var response = await _api.CreateLinkGroupAsync(model, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<Guid>(response, "[FOOTER ADMIN] Failed to create link group: {StatusCode}");
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
            var response = await _api.UpdateLinkGroupAsync(id, model, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<Guid>(response, "[FOOTER ADMIN] Failed to update link group {Id}: {StatusCode}", id);
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
            var response = await _api.DeleteLinkGroupAsync(id, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<bool>(response, "[FOOTER ADMIN] Failed to delete link group {Id}: {StatusCode}", id);
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
            var response = await _api.ReorderLinkGroupsAsync(orderedIds, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<Guid>(response, "[FOOTER ADMIN] Failed to reorder link groups: {StatusCode}");
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
            var response = await _api.CreateLinkAsync(groupId, model, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<Guid>(response, "[FOOTER ADMIN] Failed to create link in group {GroupId}: {StatusCode}", groupId);
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
            var response = await _api.UpdateLinkAsync(id, model, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<Guid>(response, "[FOOTER ADMIN] Failed to update link {Id}: {StatusCode}", id);
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
            var response = await _api.DeleteLinkAsync(id, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<bool>(response, "[FOOTER ADMIN] Failed to delete link {Id}: {StatusCode}", id);
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
            var response = await _api.GetFooterConfigAsync(CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[FOOTER ADMIN] Failed to fetch footer config: {StatusCode}", (int)response.StatusCode);
                return null;
            }

            return response.Content?.Settings;
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
            var response = await _api.UpdateTenantSettingsAsync(model, CancellationToken.None);
            return response.IsSuccessStatusCode
                ? response.Content
                : CreateFailureResponse<Guid>(response, "[FOOTER ADMIN] Failed to update tenant footer settings: {StatusCode}");
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
