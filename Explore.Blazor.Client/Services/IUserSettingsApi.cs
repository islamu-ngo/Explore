// ABOUTME: Refit interface and tolerant response DTOs for user settings BFF reads.
// ABOUTME: Handles API string-enum setting sources while preserving generated client DTOs for UI callers.

using System.Text.Json;
using Refit;

namespace Explore.Blazor.Client.Services;

public interface IUserSettingsApi
{
    [Get("/api/settings/user/{category}")]
    Task<IApiResponse<UserSettingGroupApiResponse>> GetUserSettingsAsync(
        string category,
        CancellationToken cancellationToken);
}

public sealed class UserSettingGroupApiResponse
{
    public string Category { get; set; } = string.Empty;
    public List<UserEffectiveSettingApiResponse> Settings { get; set; } = [];
}

public sealed class UserEffectiveSettingApiResponse
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int? SettingValueTypeId { get; set; }
    public string SettingValueTypeCode { get; set; } = string.Empty;
    public string SettingValueTypeName { get; set; } = string.Empty;
    public JsonElement? Source { get; set; }
    public bool? IsLocked { get; set; }
    public bool? CanEdit { get; set; }
    public string? Reason { get; set; }
    public string? Description { get; set; }
    public string? AllowedValues { get; set; }
}
