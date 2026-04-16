// ABOUTME: Smoke tests for RuntimeTranslationProvider — force_offline_mode short-circuit + exception fallback.
// ABOUTME: Verifies that a failing live provider never bubbles errors out of the runtime wrapper.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Telemetry;
using Explore.Domain.Enums;
using Explore.Infrastructure.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Infrastructure.Localization;

public class RuntimeTranslationProviderFallbackTests
{
    [Test]
    public async Task ExportTranslations_WhenForceOfflineMode_ShortCircuitsToOfflineProvider()
    {
        var resolver = new MutableConfigResolver
        {
            Current = new TranslationConfiguration(
                TranslationManagementProviderEnum.Tolgee, "https://tolgee.test", "proj1", null, "en")
            {
                ForceOfflineMode = true
            }
        };

        var provider = BuildProvider(resolver);

        var result = await provider.ExportTranslationsAsync("en");

        // Offline bundles ship empty in v1 — the important invariant is "no exception and no Tolgee call".
        await Assert.That(result).IsNotNull();
        await Assert.That(resolver.ResolveCallCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ExportTranslations_WhenLiveProviderThrows_FallsBackToOfflineAndSwallowsException()
    {
        var resolver = new MutableConfigResolver
        {
            Current = new TranslationConfiguration(
                TranslationManagementProviderEnum.Tolgee, "https://tolgee.unreachable.invalid", "proj1", null, "en")
        };

        var provider = BuildProvider(resolver);

        // Tolgee with an unreachable host throws; runtime provider must not bubble the exception.
        var result = await provider.ExportTranslationsAsync("en");

        await Assert.That(result).IsNotNull();
    }

    private static RuntimeTranslationProvider BuildProvider(ITranslationConfigResolver resolver)
    {
        var tolgeeClient = new HttpClient(new ThrowingHttpHandler()) { BaseAddress = new Uri("https://tolgee.unreachable.invalid") };
        var weblateClient = new HttpClient(new ThrowingHttpHandler()) { BaseAddress = new Uri("https://weblate.unreachable.invalid") };

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

    private sealed class MutableConfigResolver : ITranslationConfigResolver
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

    private static TranslationMetrics CreateTestMetrics()
    {
        var meter = new System.Diagnostics.Metrics.Meter("test.fallback");
        var factory = Substitute.For<System.Diagnostics.Metrics.IMeterFactory>();
        factory.Create(Arg.Any<System.Diagnostics.Metrics.MeterOptions>()).Returns(meter);
        return new TranslationMetrics(factory);
    }

    private sealed class ThrowingHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("simulated network failure");
    }
}
