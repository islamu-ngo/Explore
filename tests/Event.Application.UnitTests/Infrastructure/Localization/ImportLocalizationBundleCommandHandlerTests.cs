// ABOUTME: Unit tests for ImportLocalizationBundleCommandHandler admin containment and side effects.
// ABOUTME: Verifies static bundle imports require instance-admin authority before persistence/cache mutation.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Localization;
using Explore.Application.Features.Localization.Handlers.Commands;
using Explore.Application.Features.Localization.Requests.Commands;
using Explore.Application.Telemetry;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Infrastructure.Localization;

public class ImportLocalizationBundleCommandHandlerTests
{
    private static readonly Guid ActorId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly IAdminContext _adminContext;
    private readonly IBundleFileWriter _bundleFileWriter;
    private readonly ITranslationResolver _translationResolver;
    private readonly ImportLocalizationBundleCommandHandler _handler;

    public ImportLocalizationBundleCommandHandlerTests()
    {
        _adminContext = Substitute.For<IAdminContext>();
        _adminContext.ResolveUserIdAsync(Arg.Any<CancellationToken>()).Returns(ActorId);
        _adminContext.IsInstanceAdminAsync(ActorId, Arg.Any<CancellationToken>()).Returns(true);
        _bundleFileWriter = Substitute.For<IBundleFileWriter>();
        _translationResolver = Substitute.For<ITranslationResolver>();

        _handler = new ImportLocalizationBundleCommandHandler(
            _adminContext,
            _bundleFileWriter,
            _translationResolver,
            CreateTestMetrics(),
            Substitute.For<ILogger<ImportLocalizationBundleCommandHandler>>());
    }

    [Test]
    public async Task Handle_WhenInstanceAdmin_ImportsBundleAndInvalidatesCache()
    {
        _bundleFileWriter.WriteBundleAsync("en", Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns("/app/data/en.json");

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _bundleFileWriter.Received(1).WriteBundleAsync(
            "en",
            Arg.Is<IReadOnlyDictionary<string, string>>(translations => translations["ui.button.save"] == "Save"),
            Arg.Any<CancellationToken>());
        await _translationResolver.Received(1).InvalidateLanguageAsync("en", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenUserIsNotInstanceAdmin_DeniesBeforeWriterOrCacheInvalidation()
    {
        _adminContext.IsInstanceAdminAsync(ActorId, Arg.Any<CancellationToken>()).Returns(false);
        _bundleFileWriter.WriteBundleAsync("en", Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns("/app/data/en.json");

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).Contains("Instance administrator");
        await _bundleFileWriter.DidNotReceive().WriteBundleAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
        await _translationResolver.DidNotReceive().InvalidateLanguageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCancelledDuringAdminResolution_PropagatesCancellationBeforeWriterOrCacheInvalidation()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        _adminContext.ResolveUserIdAsync(source.Token)
            .Returns(Task.FromCanceled<Guid?>(source.Token));

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.Handle(BuildCommand(), source.Token));

        await Assert.That(exception.CancellationToken).IsEqualTo(source.Token);
        await _adminContext.Received(1).ResolveUserIdAsync(source.Token);
        await _adminContext.DidNotReceive().IsInstanceAdminAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _bundleFileWriter.DidNotReceive().WriteBundleAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
        await _translationResolver.DidNotReceive().InvalidateLanguageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static ImportLocalizationBundleCommand BuildCommand() => new()
    {
        Dto = new ImportLocalizationBundleDto
        {
            LanguageCode = "en",
            Translations = new Dictionary<string, string>
            {
                ["ui.button.save"] = "Save"
            }
        }
    };

    private static TranslationMetrics CreateTestMetrics()
    {
        var meter = new Meter(TranslationMetrics.MeterName);
        var factory = Substitute.For<IMeterFactory>();
        factory.Create(Arg.Any<MeterOptions>()).Returns(meter);
        return new TranslationMetrics(factory);
    }
}
