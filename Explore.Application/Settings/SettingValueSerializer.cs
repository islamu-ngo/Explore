// ABOUTME: Centralized serialization/deserialization for setting values stored as JSON strings.
// ABOUTME: Replaces copy-pasted DeserializeString/Int/Bool helpers across 3+ services.

namespace Explore.Application.Settings;

using System.Text.Json;

/// <summary>
/// Centralizes all setting value serialization/deserialization logic.
/// All setting values are stored as JSON-serialized strings in the database.
/// </summary>
public static class SettingValueSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserializes a JSON-serialized setting value to the target type.
    /// Returns <paramref name="defaultValue"/> when the raw value is null, empty, or malformed.
    /// </summary>
    public static T Deserialize<T>(string? rawValue, T defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return defaultValue;

        try
        {
            var result = JsonSerializer.Deserialize<T>(rawValue, Options);
            return result ?? defaultValue;
        }
        catch
        {
            return TryParseFallback(rawValue, defaultValue);
        }
    }

    /// <summary>
    /// Serializes a value to its JSON string representation for storage.
    /// </summary>
    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    /// <summary>
    /// Deserializes a string setting value, trimming JSON quotes if present.
    /// </summary>
    public static string DeserializeString(string? rawValue, string defaultValue = "")
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return defaultValue;

        try
        {
            var deserialized = JsonSerializer.Deserialize<string>(rawValue, Options);
            return string.IsNullOrWhiteSpace(deserialized) ? defaultValue : deserialized;
        }
        catch
        {
            return rawValue.Trim('"');
        }
    }

    /// <summary>
    /// Deserializes an integer setting value with fallback parsing.
    /// </summary>
    public static int DeserializeInt(string? rawValue, int defaultValue = 0)
        => Deserialize(rawValue, defaultValue);

    /// <summary>
    /// Deserializes a long integer setting value with fallback parsing.
    /// </summary>
    public static long DeserializeLong(string? rawValue, long defaultValue = 0)
        => Deserialize(rawValue, defaultValue);

    /// <summary>
    /// Deserializes a boolean setting value with fallback parsing.
    /// </summary>
    public static bool DeserializeBool(string? rawValue, bool defaultValue = false)
        => Deserialize(rawValue, defaultValue);

    /// <summary>
    /// Deserializes a decimal setting value with fallback parsing.
    /// </summary>
    public static decimal DeserializeDecimal(string? rawValue, decimal defaultValue = 0m)
        => Deserialize(rawValue, defaultValue);

    private static T TryParseFallback<T>(string rawValue, T defaultValue)
    {
        if (typeof(T) == typeof(int) && int.TryParse(rawValue, out var intVal))
            return (T)(object)intVal;

        if (typeof(T) == typeof(bool) && bool.TryParse(rawValue, out var boolVal))
            return (T)(object)boolVal;

        if (typeof(T) == typeof(decimal) && decimal.TryParse(rawValue, out var decVal))
            return (T)(object)decVal;

        if (typeof(T) == typeof(string))
            return (T)(object)rawValue.Trim('"');

        if (typeof(T) == typeof(DateTime) && DateTime.TryParse(rawValue, out var dtVal))
            return (T)(object)dtVal;

        return defaultValue;
    }
}
