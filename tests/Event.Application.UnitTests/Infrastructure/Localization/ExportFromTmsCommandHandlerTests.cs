// ABOUTME: Unit tests for ExportFromTmsCommandHandler — bundle persistence, invalidation, error handling.
// ABOUTME: Verifies IBundleFileWriter is called with correct dict, resolver invalidation fires, BundleWriteException handled.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Localization.Handlers.Commands;
using Explore.Application.Features.Localization.Requests.Commands;
using Explore.Application.Telemetry;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Event.Application.UnitTests.Infrastructure.Localization;

public class ExportFromTmsCommandHandlerTests
{
    private readonly ITranslationManagementProvider _tmsProvider;
    private readonly ITranslationResolver _translationResolver;
    private readonly IBundleFileWriter _bundleFileWriter;
    private readonly TranslationMetrics _metrics;
    private readonly ExportFromTmsCommandHandler _handler;

    public ExportFromTmsCommandHandlerTests()
    {
        _tmsProvider = Substitute.For<ITranslationManagementProvider>();
        _translationResolver = Substitute.For<ITranslationResolver>();
        _bundleFileWriter = Substitute.For<IBundleFileWriter>();
        _metrics = CreateTestMetrics();

        _handler = new ExportFromTmsCommandHandler(
            _tmsProvider,
            _translationResolver,
            _bundleFileWriter,
            _metrics,
            Substitute.For<ILogger<ExportFromTmsCommandHandler>>());
    }

    [Test]
    public async Task Handle_WithTranslations_CallsWriterWithCorrectDictionary()
    {
        var exports = new List<TranslationExport>
        {
            new("ui.button.save", "Save"),
            new("ui.button.cancel", "Cancel")
        };
        _tmsProvider.ExportTranslationsAsync("en", Arg.Any<CancellationToken>())
            .Returns(exports);
        _bundleFileWriter.WriteBundleAsync("en", Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns("/app/data/en.json");

        var result = await _handler.Handle(new ExportFromTmsCommand { LanguageCode = "en" }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).Contains("2 translations");
        await _bundleFileWriter.Received(1).WriteBundleAsync(
            "en",
            Arg.Is<IReadOnlyDictionary<string, string>>(d => d.Count == 2 && d["ui.button.save"] == "Save"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_AfterWrite_InvalidatesResolverCache()
    {
        _tmsProvider.ExportTranslationsAsync("fr", Arg.Any<CancellationToken>())
            .Returns(new[] { new TranslationExport("key1", "value1") });
        _bundleFileWriter.WriteBundleAsync("fr", Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns("/app/data/fr.json");

        await _handler.Handle(new ExportFromTmsCommand { LanguageCode = "fr" }, CancellationToken.None);

        await _translationResolver.Received(1).InvalidateLanguageAsync("fr", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenBundleWriteThrows_ReturnsFailureWithMessage()
    {
        _tmsProvider.ExportTranslationsAsync("ar", Arg.Any<CancellationToken>())
            .Returns(new[] { new TranslationExport("key1", "value1") });
        _bundleFileWriter.WriteBundleAsync("ar", Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Throws(new BundleWriteException("Disk full"));

        var result = await _handler.Handle(new ExportFromTmsCommand { LanguageCode = "ar" }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Disk full");
    }

    [Test]
    public async Task Handle_EmptyExports_ReturnsFailure()
    {
        _tmsProvider.ExportTranslationsAsync("en", Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<TranslationExport>());

        var result = await _handler.Handle(new ExportFromTmsCommand { LanguageCode = "en" }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("No translations found");
        await _bundleFileWriter.DidNotReceive().WriteBundleAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenTmsProviderThrows_ReturnsFailure()
    {
        _tmsProvider.ExportTranslationsAsync("en", Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("TMS unreachable"));

        var result = await _handler.Handle(new ExportFromTmsCommand { LanguageCode = "en" }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("TMS unreachable");
    }

    [Test]
    public async Task Handle_DuplicateKeys_TakesFirstValue()
    {
        var exports = new List<TranslationExport>
        {
            new("ui.button.save", "Save"),
            new("ui.button.save", "Save (duplicate)")
        };
        _tmsProvider.ExportTranslationsAsync("en", Arg.Any<CancellationToken>())
            .Returns(exports);
        _bundleFileWriter.WriteBundleAsync("en", Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns("/app/data/en.json");

        var result = await _handler.Handle(new ExportFromTmsCommand { LanguageCode = "en" }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _bundleFileWriter.Received(1).WriteBundleAsync(
            "en",
            Arg.Is<IReadOnlyDictionary<string, string>>(d => d.Count == 1 && d["ui.button.save"] == "Save"),
            Arg.Any<CancellationToken>());
    }

    private static TranslationMetrics CreateTestMetrics()
    {
        var meter = new Meter(TranslationMetrics.MeterName);
        var factory = Substitute.For<IMeterFactory>();
        factory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        return new TranslationMetrics(factory);
    }
}
