// ABOUTME: Interface for encryption service with key versioning support.
// Used for encrypting database-stored settings with rotation capability.

namespace Explore.Secrets.Abstractions;

/// <summary>
/// Encryption result containing ciphertext and key version used.
/// </summary>
/// <param name="Ciphertext">Base64-encoded encrypted value.</param>
/// <param name="KeyVersion">Version of the encryption key used.</param>
public sealed record EncryptionResult(string Ciphertext, int KeyVersion);

/// <summary>
/// Provides encryption and decryption with key versioning for rotation support.
/// Uses AES-256-GCM for authenticated encryption.
/// </summary>
public interface IEncryptionService : IDisposable
{
    /// <summary>
    /// Gets the current key version used for new encryptions.
    /// </summary>
    int CurrentKeyVersion { get; }

    /// <summary>
    /// Encrypts plaintext using the current key version.
    /// </summary>
    /// <param name="plaintext">The value to encrypt.</param>
    /// <returns>Encryption result with ciphertext and key version.</returns>
    /// <exception cref="InvalidOperationException">When encryption fails.</exception>
    EncryptionResult Encrypt(string plaintext);

    /// <summary>
    /// Decrypts ciphertext using the specified key version.
    /// </summary>
    /// <param name="ciphertext">Base64-encoded encrypted value.</param>
    /// <param name="keyVersion">The key version used during encryption.</param>
    /// <returns>The decrypted plaintext.</returns>
    /// <exception cref="InvalidOperationException">When decryption fails or key version not found.</exception>
    string Decrypt(string ciphertext, int keyVersion);

    /// <summary>
    /// Checks if a specific key version is available for decryption.
    /// </summary>
    /// <param name="keyVersion">The key version to check.</param>
    /// <returns>True if the key version is available.</returns>
    bool HasKeyVersion(int keyVersion);

    /// <summary>
    /// Gets all available key versions (for re-encryption planning).
    /// </summary>
    IReadOnlyCollection<int> AvailableKeyVersions { get; }
}
