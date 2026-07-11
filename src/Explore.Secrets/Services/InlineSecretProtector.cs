// ABOUTME: Data Protection implementation of the Application inline-secret protection contract.
// ABOUTME: Reuses InlineSecretSource purpose/version so protected SecretBinding values can be resolved later.

namespace Explore.Secrets.Services;

using Explore.Application.Contracts.Secrets;
using Explore.Secrets.Sources;
using Microsoft.AspNetCore.DataProtection;

public sealed class InlineSecretProtector(IDataProtectionProvider provider) : IInlineSecretProtector
{
    private const int CurrentVersion = 1;

    public InlineProtectedSecret Protect(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        return new InlineProtectedSecret(InlineSecretSource.Protect(provider, plaintext), CurrentVersion);
    }
}
