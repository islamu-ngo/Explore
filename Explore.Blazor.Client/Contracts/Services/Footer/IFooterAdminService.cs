// ABOUTME: Contract for managing footer link groups and links via the API.
// ABOUTME: Models are defined locally since NSwag client has not been regenerated for footer endpoints.

using Explore.Blazor.Client.Models.Responses;

namespace Explore.Blazor.Client.Contracts.Services.Footer;

/// <summary>
/// Service interface for managing footer link groups, links, and tenant footer settings.
/// </summary>
public interface IFooterAdminService
{
    // ── Settings (read) ──────────────────────────────────────────────────

    Task<FooterSettingsResponseModel?> GetFooterSettingsAsync();

    // ── Link Groups ──────────────────────────────────────────────────────

    Task<List<FooterLinkGroupListModel>> GetLinkGroupsAsync();
    Task<FooterLinkGroupDetailsModel?> GetLinkGroupAsync(Guid id);
    Task<BaseCommandResponse<Guid>?> CreateLinkGroupAsync(CreateFooterLinkGroupModel model);
    Task<BaseCommandResponse<Guid>?> UpdateLinkGroupAsync(Guid id, UpdateFooterLinkGroupModel model);
    Task<BaseCommandResponse<bool>?> DeleteLinkGroupAsync(Guid id);
    Task<BaseCommandResponse<Guid>?> ReorderLinkGroupsAsync(List<Guid> orderedIds);

    // ── Links ────────────────────────────────────────────────────────────

    Task<BaseCommandResponse<Guid>?> CreateLinkAsync(Guid groupId, CreateFooterLinkModel model);
    Task<BaseCommandResponse<Guid>?> UpdateLinkAsync(Guid id, UpdateFooterLinkModel model);
    Task<BaseCommandResponse<bool>?> DeleteLinkAsync(Guid id);

    // ── Tenant Footer Settings ───────────────────────────────────────────

    Task<BaseCommandResponse<Guid>?> UpdateTenantSettingsAsync(UpdateTenantFooterSettingsModel model);
}

// ── Response Models ──────────────────────────────────────────────────────

public class FooterLinkGroupListModel
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public int LinkCount { get; set; }
}

public class FooterLinkGroupDetailsModel
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<FooterLinkDetailModel> Links { get; set; } = new();
}

public class FooterLinkDetailModel
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool OpenInNewTab { get; set; }
    public int Order { get; set; }
}

// ── Request Models ───────────────────────────────────────────────────────

public class CreateFooterLinkGroupModel
{
    public string Title { get; set; } = string.Empty;
}

public class UpdateFooterLinkGroupModel
{
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateFooterLinkModel
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool OpenInNewTab { get; set; }
}

public class UpdateFooterLinkModel
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool OpenInNewTab { get; set; }
    public bool IsActive { get; set; }
}

// ── Settings Response Models ─────────────────────────────────────────────

public class FooterSettingsResponseModel
{
    public bool Enabled { get; set; }
    public string Template { get; set; } = "standard-3-col";
    public bool ShowDescription { get; set; }
    public string DescriptionText { get; set; } = string.Empty;
    public bool ShowSocialLinks { get; set; }
    public List<FooterSocialLinkResponseModel> SocialLinks { get; set; } = new();
    public string CopyrightText { get; set; } = string.Empty;
    public bool ShowCookieSettingsLink { get; set; }
}

public class FooterSocialLinkResponseModel
{
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class UpdateTenantFooterSettingsModel
{
    public bool? Enabled { get; set; }
    public string? Template { get; set; }
    public bool? ShowDescription { get; set; }
    public string? DescriptionText { get; set; }
    public bool? ShowSocialLinks { get; set; }
    public string? SocialLinksJson { get; set; }
    public string? CopyrightText { get; set; }
    public bool? ShowCookieSettingsLink { get; set; }
}
