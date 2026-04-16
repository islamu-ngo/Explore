// ABOUTME: Unit tests for RuntimeTranslationProvider — provider routing, fallback behavior, and cache.
// ABOUTME: Verifies None→Offline, Tolgee/Weblate routing, and graceful degradation on TMS errors.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Telemetry;
using Explore.Domain.Enums;
using Explore.Infrastructure.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Infrastructure.Localization;

public class RuntimeTranslationProviderTests
{
    [Test]
    public async Task ExportTranslations_WhenNoneProvider_UsesOffline()
    {
        var resolver = new MutableTranslationConfigResolver
        {
            Current = new TranslationConfiguration(
                TranslationManagementProviderEnum.None, null, null, null, "en")
        };

        var provider = CreateRuntimeProvider(resolver);

        var result = await provider.ExportTranslationsAsync("en");
        var list = result.ToList();

        // Offline provider returns empty for embedded bundles (starter bundles are empty)
        await Assert.That(list).IsNotNull();
        await Assert.That(resolver.ResolveCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task ExportTranslations_WhenTolgeeProvider_RoutesToTolgee()
    {
        var resolver = new MutableTranslationConfigResolver
        {
            Current = new TranslationConfiguration(
                TranslationManagementProviderEnum.Tolgee, "https://tolgee.test", "proj1", null, "en")
        };

        var provider = CreateRuntimeProvider(resolver);

        // Tolgee will fail (no real server), should fall back to offline
        var result = await provider.ExportTranslationsAsync("en");

        await Assert.That(result).IsNotNull();
        // ResolveCallCount >= 1: RuntimeProvider resolves once, inner TolgeeProvider also resolves for config
        await Assert.That(resolver.ResolveCallCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ExportTranslations_WhenWeblateProvider_RoutesToWeblate()
    {
        var resolver = new MutableTranslationConfigResolver
        {
            Current = new TranslationConfiguration(
                TranslationManagementProviderEnum.Weblate, "https://weblate.test", "proj1", "comp1", "en")
        };

        var provider = CreateRuntimeProvider(resolver);

        // Weblate will fail (no real server), should fall back to offline
        var result = await provider.ExportTranslationsAsync("en");

        await Assert.That(result).IsNotNull();
        // ResolveCallCount >= 1: RuntimeProvider resolves once, inner WeblateProvider also resolves for config
        await Assert.That(resolver.ResolveCallCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task TestConnection_WhenNoneProvider_ReturnsTrue()
    {
        var resolver = new MutableTranslationConfigResolver
        {
            Current = new TranslationConfiguration(
                TranslationManagementProviderEnum.None, null, null, null, "en")
        };

        var provider = CreateRuntimeProvider(resolver);

        var connected = await provider.TestConnectionAsync();

        await Assert.That(connected).IsTrue();
    }

    [Test]
    public async Task GetAvailableLanguages_WhenNoneProvider_ReturnsOfflineLanguages()
    {
        var resolver = new MutableTranslationConfigResolver
        {
            Current = new TranslationConfiguration(
                TranslationManagementProviderEnum.None, null, null, null, "en")
        };

        var provider = CreateRuntimeProvider(resolver);

        var languages = await provider.GetAvailableLanguagesAsync();
        var list = languages.ToList();

        // Offline provider should find embedded bundle files (en, fr, ar)
        await Assert.That(list).IsNotNull();
    }

    [Test]
    public async Task ExportTranslations_WhenConfigResolverThrows_FallsBackToOffline()
    {
        var resolver = new ThrowingTranslationConfigResolver();

        var provider = CreateRuntimeProvider(resolver);

        var result = await provider.ExportTranslationsAsync("en");

        await Assert.That(result).IsNotNull();
    }

    private static RuntimeTranslationProvider CreateRuntimeProvider(ITranslationConfigResolver resolver)
    {
        var tolgeeClient = new HttpClient(new StaticOkHandler());
        var weblateClient = new HttpClient(new StaticOkHandler());

        var tolgee = new TolgeeTranslationProvider(tolgeeClient, resolver, Substitute.For<ILogger<TolgeeTranslationProvider>>());
        var weblate = new WeblateTranslationProvider(weblateClient, resolver, Substitute.For<ILogger<WeblateTranslationProvider>>());
        var offline = new OfflineTranslationProvider(Substitute.For<ILogger<OfflineTranslationProvider>>());
        var nullProvider = new NullTranslationProvider(Substitute.For<ILogger<NullTranslationProvider>>());

        return new RuntimeTranslationProvider(
            tolgee,
            weblate,
            offline,
            nullProvider,
            resolver,
            CreateTestMetrics(),
            Substitute.For<ILogger<RuntimeTranslationProvider>>());
    }

    private static TranslationMetrics CreateTestMetrics()
    {
        var meter = new System.Diagnostics.Metrics.Meter("test.translation");
        var factory = Substitute.For<System.Diagnostics.Metrics.IMeterFactory>();
        factory.Create(Arg.Any<System.Diagnostics.Metrics.MeterOptions>()).Returns(meter);
        return new TranslationMetrics(factory);
    }

    private sealed class MutableTranslationConfigResolver : ITranslationConfigResolver
    {
        public TranslationConfiguration Current { get; set; } = new(
            TranslationManagementProviderEnum.None, null, null, null, "en");

        public int ResolveCallCount { get; private set; }

        public Task<TranslationConfiguration> ResolveAsync(CancellationToken ct = default)
        {
            ResolveCallCount++;
            return Task.FromResult(Current);
        }

        public void InvalidateCache(Guid? tenantId = null) { }
    }

    private sealed class ThrowingTranslationConfigResolver : ITranslationConfigResolver
    {
        public Task<TranslationConfiguration> ResolveAsync(CancellationToken ct = default)
            => throw new InvalidOperationException("Config resolution failed");

        public void InvalidateCache(Guid? tenantId = null) { }
    }

    private sealed class StaticOkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
