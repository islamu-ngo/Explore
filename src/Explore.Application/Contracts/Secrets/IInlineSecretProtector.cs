// ABOUTME: Application-layer abstraction for encrypting inline SecretBinding values without depending on Explore.Secrets.
// ABOUTME: Keeps CQRS handlers clean while the Secrets project owns Data Protection implementation details.

namespace Explore.Application.Contracts.Secrets;

public interface IInlineSecretProtector
{
    InlineProtectedSecret Protect(string plaintext);
}

public sealed record InlineProtectedSecret
{
    public InlineProtectedSecret(ReadOnlyMemory<byte> Ciphertext, int Version)
    {
        this.Ciphertext = Ciphertext.ToArray();
        this.Version = Version;
    }

    public ReadOnlyMemory<byte> Ciphertext { get; }
    public int Version { get; }
}
