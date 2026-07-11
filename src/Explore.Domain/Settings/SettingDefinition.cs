// ABOUTME: Immutable definition of a single setting: its key, type, default, allowed scopes, and constraints.
// ABOUTME: Part of the code-defined Setting Definition Registry — setting metadata lives in code, not the database.

namespace Explore.Domain.Settings;

/// <summary>
/// Defines the metadata for a single setting in the hierarchical settings engine.
/// Definitions are registered in <see cref="SettingRegistry"/> at startup and are immutable.
/// </summary>
/// <param name="Key">Unique dot-notation key (e.g., "email.smtp_host").</param>
/// <param name="ValueType">Data type of the value for serialization and validation.</param>
/// <param name="DefaultValue">JSON-serialized default value used when no scope override exists.</param>
/// <param name="Category">Grouping category for admin UI display (e.g., "Email", "Branding").</param>
/// <param name="Description">Human-readable description of the setting.</param>
/// <param name="MinScope">Broadest scope at which this setting can be set. Defaults to Instance.</param>
/// <param name="MaxScope">Narrowest scope at which this setting can be overridden. Defaults to Tenant.</param>
/// <param name="IsLockable">Whether a parent scope can lock this setting to prevent child overrides.</param>
/// <param name="IsSensitive">Whether this setting contains credentials or secrets requiring masked display.</param>
/// <param name="AllowedValues">Optional constrained set of allowed values (JSON-serialized strings).</param>
public sealed record SettingDefinition(
    string Key,
    SettingValueType ValueType,
    string DefaultValue,
    string Category,
    string Description,
    SettingScope MinScope = SettingScope.Instance,
    SettingScope MaxScope = SettingScope.Tenant,
    bool IsLockable = true,
    bool IsSensitive = false,
    string[]? AllowedValues = null);
