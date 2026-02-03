// ABOUTME: AES-256-GCM encryption service with multi-version key support for rotation.
// Implements secure memory handling with CryptographicOperations.ZeroMemory().

namespace Explore.Secrets.Services;

using System.Security.Cryptography;
using System.Text;
using Explore.Secrets.Abstractions;
using Explore.Secrets.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// AES-256-GCM encryption service supporting multiple key versions for rotation.
/// </summary>
/// <remarks>
/// Format of encrypted data: base64(nonce[12] + tag[16] + ciphertext)
/// - Nonce: 12 bytes (96 bits) - randomly generated per encryption
/// - Tag: 16 bytes (128 bits) - authentication tag
/// - Ciphertext: variable length - encrypted data
/// </remarks>
public sealed class AesEncryptionService : IEncryptionService
{
    private const int NonceSize = 12; // 96 bits - AES-GCM standard
    private const int TagSize = 16;   // 128 bits - maximum authentication tag
    private const int KeySize = 32;   // 256 bits - AES-256

    private readonly Dictionary<int, byte[]> _keyVersions;
    private readonly int _currentKeyVersion;
    private readonly ILogger<AesEncryptionService>? _logger;
    private bool _disposed;

    /// <summary>
    /// Creates an AES encryption service from options.
    /// </summary>
    /// <param name="options">Encryption options with key versions.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public AesEncryptionService(
        IOptions<EncryptionOptions> options,
        ILogger<AesEncryptionService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Value);

        _logger = logger;
        _keyVersions = new Dictionary<int, byte[]>();
        var config = options.Value;

        // Load keys from options
        if (config.KeyVersions.Count > 0)
        {
            foreach (var (version, base64Key) in config.KeyVersions)
            {
                var keyBytes = DecodeAndValidateKey(base64Key, version);
                _keyVersions[version] = keyBytes;
            }
        }
        // Auto-load from environment if configured and no keys provided
        else if (config.AutoLoadFromEnvironment)
        {
            var envKey = Environment.GetEnvironmentVariable(config.MasterKeyEnvironmentVariable);
            if (!string.IsNullOrEmpty(envKey))
            {
                var keyBytes = DecodeAndValidateKey(envKey, 1);
                _keyVersions[1] = keyBytes;
                _logger?.LogDebug("Loaded encryption key version 1 from environment variable");
            }
        }

        if (_keyVersions.Count == 0)
        {
            throw new InvalidOperationException(
                "No encryption keys configured. Provide keys via EncryptionOptions.KeyVersions " +
                $"or set environment variable '{config.MasterKeyEnvironmentVariable}'.");
        }

        _currentKeyVersion = config.CurrentKeyVersion;
        if (!_keyVersions.ContainsKey(_currentKeyVersion))
        {
            throw new InvalidOperationException(
                $"Current key version {_currentKeyVersion} not found in available keys. " +
                $"Available versions: {string.Join(", ", _keyVersions.Keys)}");
        }

        _logger?.LogInformation(
            "AES encryption service initialized with {Count} key version(s), current version: {Version}",
            _keyVersions.Count,
            _currentKeyVersion);
    }

    /// <summary>
    /// Creates an AES encryption service with explicit keys (for testing).
    /// </summary>
    /// <param name="keyVersions">Dictionary of version to 32-byte key.</param>
    /// <param name="currentKeyVersion">Current version for new encryptions.</param>
    /// <param name="logger">Optional logger.</param>
    public AesEncryptionService(
        Dictionary<int, byte[]> keyVersions,
        int currentKeyVersion,
        ILogger<AesEncryptionService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(keyVersions);

        if (keyVersions.Count == 0)
        {
            throw new ArgumentException("At least one key version is required.", nameof(keyVersions));
        }

        _logger = logger;
        _keyVersions = new Dictionary<int, byte[]>();

        foreach (var (version, key) in keyVersions)
        {
            if (key.Length != KeySize)
            {
                throw new ArgumentException(
                    $"Key version {version} must be exactly {KeySize} bytes (256 bits), got {key.Length} bytes.",
                    nameof(keyVersions));
            }

            // Copy keys to prevent external modification
            var keyCopy = new byte[KeySize];
            key.CopyTo(keyCopy, 0);
            _keyVersions[version] = keyCopy;
        }

        if (!_keyVersions.ContainsKey(currentKeyVersion))
        {
            throw new ArgumentException(
                $"Current key version {currentKeyVersion} not found in provided keys.",
                nameof(currentKeyVersion));
        }

        _currentKeyVersion = currentKeyVersion;
    }

    /// <inheritdoc />
    public int CurrentKeyVersion => _currentKeyVersion;

    /// <inheritdoc />
    public IReadOnlyCollection<int> AvailableKeyVersions => _keyVersions.Keys.ToList().AsReadOnly();

    /// <inheritdoc />
    public EncryptionResult Encrypt(string plaintext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(plaintext);

        var key = _keyVersions[_currentKeyVersion];
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        try
        {
            // Allocate buffers
            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var ciphertext = new byte[plaintextBytes.Length];

            // Generate cryptographically secure nonce
            RandomNumberGenerator.Fill(nonce);

            // Encrypt using AES-GCM
            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            // Combine: nonce + tag + ciphertext
            var result = new byte[NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

            var base64Result = Convert.ToBase64String(result);

            _logger?.LogDebug(
                "Encrypted {Length} bytes using key version {Version}",
                plaintextBytes.Length,
                _currentKeyVersion);

            return new EncryptionResult(base64Result, _currentKeyVersion);
        }
        finally
        {
            // Zero sensitive data
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext, int keyVersion)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(ciphertext);

        if (!_keyVersions.TryGetValue(keyVersion, out var key))
        {
            throw new InvalidOperationException(
                $"Key version {keyVersion} not found. Available versions: {string.Join(", ", _keyVersions.Keys)}");
        }

        byte[] combined;
        try
        {
            combined = Convert.FromBase64String(ciphertext);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Invalid ciphertext format: not valid base64.", ex);
        }

        if (combined.Length < NonceSize + TagSize)
        {
            throw new InvalidOperationException(
                $"Invalid ciphertext: too short. Expected at least {NonceSize + TagSize} bytes, got {combined.Length}.");
        }

        // Extract components: nonce + tag + ciphertext
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var encryptedData = new byte[combined.Length - NonceSize - TagSize];

        Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(combined, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(combined, NonceSize + TagSize, encryptedData, 0, encryptedData.Length);

        var plaintextBytes = new byte[encryptedData.Length];

        try
        {
            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Decrypt(nonce, encryptedData, tag, plaintextBytes);

            var plaintext = Encoding.UTF8.GetString(plaintextBytes);

            _logger?.LogDebug(
                "Decrypted {Length} bytes using key version {Version}",
                encryptedData.Length,
                keyVersion);

            return plaintext;
        }
        catch (AuthenticationTagMismatchException ex)
        {
            throw new InvalidOperationException(
                "Decryption failed: authentication tag mismatch. Data may be corrupted or tampered.", ex);
        }
        finally
        {
            // Zero sensitive data
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    /// <inheritdoc />
    public bool HasKeyVersion(int keyVersion)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _keyVersions.ContainsKey(keyVersion);
    }

    /// <summary>
    /// Decodes and validates a base64-encoded encryption key.
    /// </summary>
    private static byte[] DecodeAndValidateKey(string base64Key, int version)
    {
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(base64Key);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Key version {version} is not valid base64.", ex);
        }

        if (keyBytes.Length != KeySize)
        {
            throw new InvalidOperationException(
                $"Key version {version} must be exactly {KeySize} bytes (256 bits), " +
                $"got {keyBytes.Length} bytes.");
        }

        return keyBytes;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Zero all key material
        foreach (var key in _keyVersions.Values)
        {
            CryptographicOperations.ZeroMemory(key);
        }

        _keyVersions.Clear();
        _disposed = true;

        _logger?.LogDebug("AES encryption service disposed, key material zeroed");
    }
}
