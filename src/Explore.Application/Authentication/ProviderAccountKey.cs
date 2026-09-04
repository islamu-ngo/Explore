// ABOUTME: Represents one canonical, authority-qualified external provider account identifier.
// ABOUTME: Preserves exact identity equality without email, username, role, or handle fallback.

using Explore.Domain.Enums;

namespace Explore.Application.Authentication;

public sealed record ProviderAccountKey
{
    public ProviderAccountKey(AuthenticationProviderKind providerKind, string value)
    {
        if (!Enum.IsDefined(providerKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(providerKind),
                providerKind,
                "Provider kind is outside the closed bootstrap contract.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ProviderKind = providerKind;
        Value = value;
    }

    public AuthenticationProviderKind ProviderKind { get; }
    public string Value { get; }
}
