// ABOUTME: Repository interface for first-class UI theme aggregates used by appearance settings and runtime composition.
// ABOUTME: Supports platform-owned and tenant-owned theme catalogs without storing themes in generic settings rows.

namespace Explore.Application.Contracts.Persistence;

using Explore.Domain;

public interface IUiThemeRepository : IGenericRepository<UiTheme, Guid>
{
    Task ClearDefaultAsync(Guid? tenantId, Guid? excludingThemeId = null);
    Task<UiTheme?> GetByThemeKeyAsync(Guid? tenantId, string themeKey);
    Task<IReadOnlyList<UiTheme>> GetOwnedThemesAsync(Guid? tenantId, bool activeOnly = false);
    Task<IReadOnlyList<UiTheme>> GetAvailableThemesForTenantAsync(Guid tenantId, bool activeOnly = true);
    Task<UiTheme?> GetDefaultThemeAsync(Guid? tenantId);
    Task<bool> ThemeKeyExistsAsync(Guid? tenantId, string themeKey, Guid? excludingThemeId = null);
}
