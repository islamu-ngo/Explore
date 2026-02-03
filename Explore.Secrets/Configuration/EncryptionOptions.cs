// ABOUTME: Configuration options for encryption service with key versioning.
// Supports multiple key versions for rotation scenarios.

namespace Explore.Secrets.Configuration;

/// <summary>
/// Configuration options for the encryption service.
/// Supports multiple key versions for rotation.
/// </summary>
public sealed class EncryptionOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Encryption";

    /// <summary>
    /// The current key version used for new encryptions.
    /// Must exist in KeyVersions dictionary.
    /// </summary>
    public int CurrentKeyVersion { get; set; } = 1;

    /// <summary>
    /// Dictionary of key versions to base64-encoded 256-bit keys.
    /// Key: version number, Value: base64 encoded 32-byte key.
    /// </summary>
    /// <example>
    /// {
    ///   "1": "base64-encoded-32-byte-key-v1",
    ///   "2": "base64-encoded-32-byte-key-v2"
    /// }
    /// </example>
    public Dictionary<int, string> KeyVersions { get; set; } = new();

    /// <summary>
    /// Environment variable name for the master key (Version 1).
    /// Used when KeyVersions is empty.
    /// </summary>
    public string MasterKeyEnvironmentVariable { get; set; } = "SECURITY__MASTERKEY";

    /// <summary>
    /// Whether to automatically load Version 1 key from environment variable
    /// if KeyVersions is empty.
    /// </summary>
    public bool AutoLoadFromEnvironment { get; set; } = true;
}
