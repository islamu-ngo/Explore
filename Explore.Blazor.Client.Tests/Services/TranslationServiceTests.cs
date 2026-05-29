// ABOUTME: Unit tests for the Blazor client TranslationService cache and fallback behavior.
// ABOUTME: Verifies registry validation, API fetch boundaries, language-change events, and hot-path lookup safety.

using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class TranslationServiceTests : IDisposable
{
    private readonly IEventApiClient _apiClient = Substitute.For<IEventApiClient>();
    private readonly TranslationService _service;

    public TranslationServiceTests()
    {
        _service = new TranslationService(_apiClient, NullLogger<TranslationService>.Instance);
    }

    public void Dispose() => _service.Dispose();

    [Test]
    public async Task T_WithCachedTranslation_ReturnsValue()
    {
        var translations = new Dictionary<string, string>
        {
            ["ui.nav.events"] = "Events"
        };
        _apiClient.GetTranslationByLanguageAsync("en", null, null, Arg.Any<CancellationToken>())
            .Returns(translations);

        await _service.GetTranslationsAsync("en");

        var result = _service.T("ui.nav.events");

        await Assert.That(result).IsEqualTo("Events");
    }

    [Test]
    public async Task T_WhenKeyMissing_ReturnsFallbackOrKey()
    {
        _apiClient.GetTranslationByLanguageAsync("en", null, null, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());
        await _service.GetTranslationsAsync("en");

        await Assert.That(_service.T("ui.missing", "Fallback")).IsEqualTo("Fallback");
        await Assert.That(_service.T("ui.missing")).IsEqualTo("ui.missing");
        await Assert.That(_service.T(null!, "Fallback")).IsEqualTo("Fallback");
    }

    [Test]
    public async Task GetTranslationsAsync_WhenLanguageIsAllowed_FetchesAndCachesCurrentLanguage()
    {
        var translations = new Dictionary<string, string>
        {
            ["ui.home.title"] = "Home"
        };
        _apiClient.GetTranslationByLanguageAsync("en", null, null, Arg.Any<CancellationToken>())
            .Returns(translations);

        var first = await _service.GetTranslationsAsync("en");
        var second = await _service.GetTranslationsAsync("en");

        await Assert.That(first["ui.home.title"]).IsEqualTo("Home");
        await Assert.That(second).IsSameReferenceAs(first);
        await _apiClient.Received(1)
            .GetTranslationByLanguageAsync("en", null, null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetTranslationsAsync_WhenLanguageIsUnknown_ReturnsEmptyAndDoesNotCallApi()
    {
        var result = await _service.GetTranslationsAsync("<script>");

        await Assert.That(result).IsEmpty();
        await _apiClient.DidNotReceiveWithAnyArgs()
            .GetTranslationByLanguageAsync(default!, default, default, default);
    }

    [Test]
    public async Task GetTranslationsAsync_WhenApiThrows_ReturnsExistingCache()
    {
        var cached = new Dictionary<string, string>
        {
            ["ui.cached"] = "Cached"
        };
        var throwOnNextFetch = false;
        _apiClient.GetTranslationByLanguageAsync("en", null, null, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (throwOnNextFetch)
                    throw new HttpRequestException("network failed");

                return Task.FromResult<IDictionary<string, string>>(cached);
            });

        await _service.GetTranslationsAsync("en");
        throwOnNextFetch = true;
        typeof(TranslationService)
            .GetField("_currentLanguage", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_service, "fr");
        var result = await _service.GetTranslationsAsync("en");

        await Assert.That(result["ui.cached"]).IsEqualTo("Cached");
    }

    [Test]
    public async Task GetTranslationsAsync_WhenApiThrowsWithoutCache_ReturnsEmptyDictionary()
    {
        _apiClient.GetTranslationByLanguageAsync("en", null, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("network failed"));

        var result = await _service.GetTranslationsAsync("en");

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetTranslationsAsync_ConcurrentColdCalls_SerializeThroughSingleApiFetch()
    {
        var fetchCount = 0;
        _apiClient.GetTranslationByLanguageAsync("en", null, null, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref fetchCount);
                return Task.Run<IDictionary<string, string>>(async () =>
                {
                    await Task.Delay(50);
                    return new Dictionary<string, string> { ["ui.concurrent"] = "Loaded" };
                });
            });

        var firstTask = _service.GetTranslationsAsync("en");
        var secondTask = _service.GetTranslationsAsync("en");
        var results = await Task.WhenAll(firstTask, secondTask);

        await Assert.That(results[0]["ui.concurrent"]).IsEqualTo("Loaded");
        await Assert.That(results[1]["ui.concurrent"]).IsEqualTo("Loaded");
        await Assert.That(fetchCount).IsEqualTo(1);
    }

    [Test]
    public async Task ChangeLanguageAsync_WhenLanguageChanges_ClearsCacheFetchesAndRaisesEvent()
    {
        _apiClient.GetTranslationByLanguageAsync("fr", null, null, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["ui.language"] = "Francais" });
        var observed = new List<string>();
        _service.OnLanguageChanged += observed.Add;

        await _service.ChangeLanguageAsync("fr");

        await Assert.That(_service.CurrentLanguage).IsEqualTo("fr");
        await Assert.That(observed).IsEquivalentTo(["fr"]);
        await Assert.That(_service.T("ui.language")).IsEqualTo("Francais");
        await _apiClient.Received(1)
            .GetTranslationByLanguageAsync("fr", null, null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ChangeLanguageAsync_WhenLanguageIsSame_DoesNotFetchOrRaiseEvent()
    {
        var observed = new List<string>();
        _service.OnLanguageChanged += observed.Add;

        await _service.ChangeLanguageAsync("en");

        await Assert.That(observed).IsEmpty();
        await _apiClient.DidNotReceiveWithAnyArgs()
            .GetTranslationByLanguageAsync(default!, default, default, default);
    }

    [Test]
    public async Task ChangeLanguageAsync_WhenLanguageIsUnknown_IsNoOp()
    {
        await _service.ChangeLanguageAsync("de");

        await Assert.That(_service.CurrentLanguage).IsEqualTo("en");
        await _apiClient.DidNotReceiveWithAnyArgs()
            .GetTranslationByLanguageAsync(default!, default, default, default);
    }

    [Test]
    public async Task PreloadAsync_WhenLanguageIsUnknown_FallsBackToEnglish()
    {
        _apiClient.GetTranslationByLanguageAsync("en", null, null, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["ui.preload"] = "Loaded" });

        await _service.PreloadAsync("de");

        await Assert.That(_service.CurrentLanguage).IsEqualTo("en");
        await Assert.That(_service.T("ui.preload")).IsEqualTo("Loaded");
    }

    [Test]
    public async Task GetAvailableLanguagesAsync_CachesApiResponse()
    {
        _apiClient.GetAvailableTranslationLanguagesAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "en", "fr" });

        var first = await _service.GetAvailableLanguagesAsync();
        var second = await _service.GetAvailableLanguagesAsync();

        await Assert.That(first).IsEquivalentTo(["en", "fr"]);
        await Assert.That(second).IsSameReferenceAs(first);
        await _apiClient.Received(1)
            .GetAvailableTranslationLanguagesAsync(null, null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAvailableLanguagesAsync_WhenApiThrowsWithoutCache_ReturnsEnglishFallback()
    {
        _apiClient.GetAvailableTranslationLanguagesAsync(null, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("network failed"));

        var result = await _service.GetAvailableLanguagesAsync();

        await Assert.That(result).IsEquivalentTo(["en"]);
    }
}
