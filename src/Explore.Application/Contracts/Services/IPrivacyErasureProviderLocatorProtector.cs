// ABOUTME: Protects short-lived remote locators before privacy-erasure provider work is persisted.
// ABOUTME: Keeps encryption and key lifecycle outside Domain and Application orchestration.

namespace Explore.Application.Contracts.Services;

public interface IPrivacyErasureProviderLocatorProtector
{
    int CurrentVersion { get; }
    string Protect(string locator, TimeSpan lifetime);
    string Unprotect(string protectedLocator);
}
