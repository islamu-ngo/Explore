// ABOUTME: Server-side no-op for ICookieConsentInterop — JS interop unavailable during SSR.
// ABOUTME: Returns null for reads, no-ops for writes. Paired with client CookieConsentInterop.

using Explore.Blazor.Client.Contracts.Interop;

namespace Explore.Blazor.Services;

public sealed class ServerCookieConsentInterop : ICookieConsentInterop
{
    public Task<string?> ReadConsentAsync(string consentCookieKey) => Task.FromResult<string?>(null);

    public Task WriteConsentAsync(string consentCookieKey, string value, int lifetimeDays) => Task.CompletedTask;

    public Task ClearConsentAsync(string consentCookieKey) => Task.CompletedTask;
}
