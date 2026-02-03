// ABOUTME: Service for rotating encryption keys and re-encrypting database settings.
// Handles staged re-encryption with progress tracking and concurrency control.

namespace Explore.Secrets.Services;

using Explore.Secrets.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Result of a key rotation operation.
/// </summary>
/// <param name="TotalSettings">Total number of settings processed.</param>
/// <param name="ReEncryptedCount">Number of settings re-encrypted.</param>
/// <param name="SkippedCount">Number of settings already at current version.</param>
/// <param name="ErrorCount">Number of settings that failed to re-encrypt.</param>
/// <param name="Errors">List of error details.</param>
public sealed record KeyRotationResult(
    int TotalSettings,
    int ReEncryptedCount,
    int SkippedCount,
    int ErrorCount,
    IReadOnlyList<KeyRotationError> Errors);

/// <summary>
/// Details of a re-encryption error.
/// </summary>
/// <param name="Key">The setting key that failed.</param>
/// <param name="OldKeyVersion">The key version that was used.</param>
/// <param name="ErrorMessage">The error message.</param>
public sealed record KeyRotationError(string Key, int OldKeyVersion, string ErrorMessage);

/// <summary>
/// Progress update during key rotation.
/// </summary>
/// <param name="Current">Current setting being processed.</param>
/// <param name="Total">Total settings to process.</param>
/// <param name="Key">Key of current setting.</param>
public sealed record KeyRotationProgress(int Current, int Total, string Key);

/// <summary>
/// Delegate for key rotation updates.
/// </summary>
public delegate Task UpdateSettingAsync(string key, string newEncryptedValue, int newKeyVersion, DateTime encryptedAt);

/// <summary>
/// Delegate for getting settings that need re-encryption.
/// </summary>
public delegate Task<IReadOnlyList<SettingToRotate>> GetSettingsToRotateAsync(int currentKeyVersion);

/// <summary>
/// Setting data needed for re-encryption.
/// </summary>
/// <param name="Key">The setting key.</param>
/// <param name="EncryptedValue">Current encrypted value.</param>
/// <param name="KeyVersion">Current key version.</param>
public sealed record SettingToRotate(string Key, string EncryptedValue, int KeyVersion);

/// <summary>
/// Service for rotating encryption keys and re-encrypting settings.
/// </summary>
public sealed class KeyRotationService
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<KeyRotationService>? _logger;

    /// <summary>
    /// Creates a new key rotation service.
    /// </summary>
    /// <param name="encryptionService">The encryption service with keys.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public KeyRotationService(
        IEncryptionService encryptionService,
        ILogger<KeyRotationService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(encryptionService);
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Re-encrypts all settings that are not using the current key version.
    /// </summary>
    /// <param name="getSettings">Delegate to fetch settings needing re-encryption.</param>
    /// <param name="updateSetting">Delegate to update a re-encrypted setting.</param>
    /// <param name="progress">Optional progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the rotation operation.</returns>
    public async Task<KeyRotationResult> ReEncryptAllAsync(
        GetSettingsToRotateAsync getSettings,
        UpdateSettingAsync updateSetting,
        IProgress<KeyRotationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(getSettings);
        ArgumentNullException.ThrowIfNull(updateSetting);

        var currentVersion = _encryptionService.CurrentKeyVersion;
        _logger?.LogInformation(
            "Starting key rotation to version {Version}",
            currentVersion);

        var settings = await getSettings(currentVersion).ConfigureAwait(false);
        var total = settings.Count;

        _logger?.LogInformation(
            "Found {Count} settings to re-encrypt",
            total);

        if (total == 0)
        {
            return new KeyRotationResult(0, 0, 0, 0, []);
        }

        var reEncryptedCount = 0;
        var skippedCount = 0;
        var errors = new List<KeyRotationError>();
        var current = 0;

        foreach (var setting in settings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            current++;
            progress?.Report(new KeyRotationProgress(current, total, setting.Key));

            // Skip if already at current version
            if (setting.KeyVersion >= currentVersion)
            {
                _logger?.LogDebug(
                    "Skipping {Key}: already at version {Version}",
                    setting.Key, setting.KeyVersion);
                skippedCount++;
                continue;
            }

            // Check if we have the old key version
            if (!_encryptionService.HasKeyVersion(setting.KeyVersion))
            {
                var error = $"Missing key version {setting.KeyVersion}";
                _logger?.LogError(
                    "Cannot re-encrypt {Key}: {Error}",
                    setting.Key, error);
                errors.Add(new KeyRotationError(setting.Key, setting.KeyVersion, error));
                continue;
            }

            try
            {
                // Decrypt with old key
                var plaintext = _encryptionService.Decrypt(
                    setting.EncryptedValue,
                    setting.KeyVersion);

                // Re-encrypt with current key
                var result = _encryptionService.Encrypt(plaintext);

                // Update in database
                await updateSetting(
                    setting.Key,
                    result.Ciphertext,
                    result.KeyVersion,
                    DateTime.UtcNow).ConfigureAwait(false);

                reEncryptedCount++;
                _logger?.LogDebug(
                    "Re-encrypted {Key}: v{OldVersion} -> v{NewVersion}",
                    setting.Key, setting.KeyVersion, result.KeyVersion);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "Failed to re-encrypt {Key}",
                    setting.Key);
                errors.Add(new KeyRotationError(setting.Key, setting.KeyVersion, ex.Message));
            }
        }

        _logger?.LogInformation(
            "Key rotation complete: {ReEncrypted} re-encrypted, {Skipped} skipped, {Errors} errors",
            reEncryptedCount, skippedCount, errors.Count);

        return new KeyRotationResult(
            total,
            reEncryptedCount,
            skippedCount,
            errors.Count,
            errors.AsReadOnly());
    }

    /// <summary>
    /// Re-encrypts a single setting.
    /// </summary>
    /// <param name="encryptedValue">Current encrypted value.</param>
    /// <param name="oldKeyVersion">Key version used for encryption.</param>
    /// <returns>New encryption result with current key version.</returns>
    public EncryptionResult ReEncryptSingle(string encryptedValue, int oldKeyVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(encryptedValue);

        if (!_encryptionService.HasKeyVersion(oldKeyVersion))
        {
            throw new InvalidOperationException(
                $"Cannot re-encrypt: key version {oldKeyVersion} not available.");
        }

        var plaintext = _encryptionService.Decrypt(encryptedValue, oldKeyVersion);
        return _encryptionService.Encrypt(plaintext);
    }
}
