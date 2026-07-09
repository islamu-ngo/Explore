// ABOUTME: Reads merged static localization bundles without calling a live TMS provider.
// ABOUTME: Used by admin export endpoints for no-TMS and self-hosted bundle workflows.

namespace Explore.Application.Contracts.Infrastructure;

public interface IStaticTranslationBundleReader
{
    Task<IReadOnlyDictionary<string, string>> ReadBundleAsync(
        string languageCode,
        CancellationToken cancellationToken = default);
}
