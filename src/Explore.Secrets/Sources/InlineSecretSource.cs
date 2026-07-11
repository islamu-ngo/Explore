// ABOUTME: Resolves secrets stored inline as ASP.NET Core Data-Protection ciphertext on the SecretBinding row.
// ABOUTME: Bootstrap secrets (e.g. postgresql.password) MUST NOT use this source — the DB cannot unlock itself.

using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Secrets;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Explore.Secrets.Sources;

/// <summary>
/// Reads inline-encrypted ciphertext from <see cref="SecretBinding.InlineCiphertext"/> and unprotects it
/// via a purpose-bound <see cref="IDataProtector"/>.
/// <para>
/// Purpose hierarchy: <c>("Event.Secrets", "Binding", "v1")</c>. Bumping the <c>v1</c> suffix is a breaking
/// migration — all existing ciphertexts must be re-encrypted under the new purpose.
/// </para>
/// </summary>
public sealed class InlineSecretSource : ISecretSource
{
    /// <summary>Canonical Data-Protection purpose segments. Keep in sync with Command handlers that Protect().</summary>
    public static readonly string[] ProtectorPurpose = new[] { "Event.Secrets", "Binding", "v1" };

    private readonly IDataProtector _protector;
    private readonly ILogger<InlineSecretSource> _logger;

    public InlineSecretSource(IDataProtectionProvider provider, ILogger<InlineSecretSource> logger)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(logger);
        _protector = provider.CreateProtector(ProtectorPurpose);
        _logger = logger;
    }

    /// <inheritdoc />
    public SecretSourceType SourceType => SecretSourceType.InlineEncrypted;

    /// <inheritdoc />
    public Task<string?> GetSecretAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binding);

        if (binding.InlineCiphertext is null || binding.InlineCiphertext.Length == 0)
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            var plaintextBytes = _protector.Unprotect(binding.InlineCiphertext);
            var plaintext = Encoding.UTF8.GetString(plaintextBytes);
            return Task.FromResult<string?>(plaintext);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(
                ex,
                "Failed to unprotect inline secret ciphertext for binding {BindingId} key {SettingKey}. Data Protection keys may have rotated or been lost.",
                binding.Id,
                binding.SettingKey);
            return Task.FromResult<string?>(null);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        var plaintext = await GetSecretAsync(binding, cancellationToken).ConfigureAwait(false);
        return !string.IsNullOrEmpty(plaintext);
    }

    /// <summary>
    /// Helper used by command handlers to produce the ciphertext bytes stored on
    /// <see cref="SecretBinding.InlineCiphertext"/>. Centralized here so the Protect/Unprotect pair share
    /// a single purpose definition.
    /// </summary>
    public static byte[] Protect(IDataProtectionProvider provider, string plaintext)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(plaintext);
        var protector = provider.CreateProtector(ProtectorPurpose);
        return protector.Protect(Encoding.UTF8.GetBytes(plaintext));
    }
}
