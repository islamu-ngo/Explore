// ABOUTME: Resolves provider-neutral storage implementations by canonical provider key.
// ABOUTME: Keeps provider selection centralized so handlers do not depend on concrete infrastructure types.

namespace Explore.Application.Contracts.Infrastructure;

public interface IFileStorageProviderResolver
{
    IFileStorageProvider GetRequired(string provider);
}
