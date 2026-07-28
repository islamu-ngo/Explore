// ABOUTME: Declares opaque guest capability-token issuance and constant-time matching primitives.
// ABOUTME: Keeps plaintext tokens one-time-only while exposing a validated hash for future persistence.

using Explore.Domain.ValueObjects;

namespace Explore.Application.Contracts.Services;

public interface IGuestCapabilityTokenService
{
    GuestCapabilityTokenIssue Issue();

    bool Matches(string? rawToken, CapabilityTokenHash expectedHash);
}

public sealed record GuestCapabilityTokenIssue(string RawToken, CapabilityTokenHash Hash);
