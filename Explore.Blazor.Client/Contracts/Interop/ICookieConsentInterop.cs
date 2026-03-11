// ABOUTME: Contract for cookie consent JS interop operations (read/write/clear consent cookie).
// ABOUTME: Client impl uses JS; server no-op returns null. Tenant-scoped via consentCookieKey.

namespace Explore.Blazor.Client.Contracts.Interop;

public interface ICookieConsentInterop
{
    Task<string?> ReadConsentAsync(string consentCookieKey);
    Task WriteConsentAsync(string consentCookieKey, string value, int lifetimeDays);
    Task ClearConsentAsync(string consentCookieKey);
}
