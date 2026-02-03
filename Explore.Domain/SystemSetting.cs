// ABOUTME: System-wide configuration setting with optional locking to prevent tenant overrides.
// Part of the 3-tier cascading settings engine (System → Tenant → Event).

namespace Explore.Domain;

/// <summary>
/// System-wide configuration setting that serves as the default value for all tenants.
/// When IsLocked is true, tenants cannot override this setting.
/// </summary>
public class SystemSetting
{
    /// <summary>
    /// Unique identifier for the setting.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique key for the setting (e.g., "events.max_sessions", "email.from_address").
    /// Uses dot notation for namespacing.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized value of the setting.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Data type of the value for validation purposes.
    /// </summary>
    public SettingValueType ValueType { get; set; }

    /// <summary>
    /// When true, tenants cannot override this setting value.
    /// Used for security policies, payment providers, and other critical settings.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// JSON array of allowed values (if constrained). Null means any value is allowed.
    /// Example: ["stripe", "paypal"] for payment providers.
    /// </summary>
    public string? AllowedValues { get; set; }

    /// <summary>
    /// Human-readable description of the setting.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category for grouping settings in UI (e.g., "Email", "Events", "Security").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Display order within the category.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// When the setting was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the setting was last modified.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Data type of a setting value for validation.
/// </summary>
public enum SettingValueType
{
    String = 0,
    Integer = 1,
    Boolean = 2,
    Decimal = 3,
    Json = 4,
    DateTime = 5
}
