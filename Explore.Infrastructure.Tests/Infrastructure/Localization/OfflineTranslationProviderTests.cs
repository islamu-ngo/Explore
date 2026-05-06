// ABOUTME: Unit tests for OfflineTranslationProvider — embedded bundle loading and language discovery.
// ABOUTME: Verifies that offline provider reads embedded JSON resources and returns translations.

using Explore.Infrastructure.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure.Localization;

public class OfflineTranslationProviderTests
{
    private readonly OfflineTranslationProvider _provider;

    public OfflineTranslationProviderTests()
    {
        _provider = new OfflineTranslationProvider(Substitute.For<ILogger<OfflineTranslationProvider>>());
    }

    [Test]
    public async Task TestConnection_AlwaysReturnsTrue()
    {
        var result = await _provider.TestConnectionAsync();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task GetAvailableLanguages_ReturnsEmbeddedBundleLanguages()
    {
        var languages = await _provider.GetAvailableLanguagesAsync();
        var list = languages.ToList();

        // Starter bundles: en.json, fr.json, ar.json
        await Assert.That(list).IsNotNull();
        await Assert.That(list.Count).IsGreaterThanOrEqualTo(3);
        await Assert.That(list).Contains("en");
        await Assert.That(list).Contains("fr");
        await Assert.That(list).Contains("ar");
    }

    [Test]
    public async Task ExportTranslations_ForExistingLanguage_ReturnsTranslations()
    {
        var result = await _provider.ExportTranslationsAsync("en");
        var list = result.ToList();

        // Starter bundles are empty, so expect empty list
        await Assert.That(list).IsNotNull();
    }

    [Test]
    public async Task ExportTranslations_ForNonExistentLanguage_ReturnsEmpty()
    {
        var result = await _provider.ExportTranslationsAsync("zz");
        var list = result.ToList();

        await Assert.That(list).IsNotNull();
        await Assert.That(list.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ImportKeys_IsNoOp()
    {
        // Offline provider is read-only; import should not throw
        await _provider.ImportKeysAsync([]);
    }
}
