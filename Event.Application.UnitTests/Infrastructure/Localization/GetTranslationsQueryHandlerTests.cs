// ABOUTME: Unit tests for GetTranslationsQueryHandler — verifies translation export via MediatR.
// ABOUTME: Tests the query handler delegates to ITranslationManagementProvider and returns dictionary.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Localization.Handlers.Queries;
using Explore.Application.Features.Localization.Requests.Queries;
using NSubstitute;

namespace Event.Application.UnitTests.Infrastructure.Localization;

public class GetTranslationsQueryHandlerTests
{
    [Test]
    public async Task Handle_ReturnsTranslationsAsDictionary()
    {
        var provider = Substitute.For<ITranslationManagementProvider>();
        provider.ExportTranslationsAsync("fr", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<TranslationExport>>(new List<TranslationExport>
            {
                new("lookup.tag.FIQH.full_name", "Jurisprudence islamique"),
                new("lookup.madhab.HANAFI.full_name", "Hanafite"),
            }));

        var handler = new GetTranslationsQueryHandler(provider);

        var result = await handler.Handle(
            new GetTranslationsQuery { LanguageCode = "fr" },
            CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result["lookup.tag.FIQH.full_name"]).IsEqualTo("Jurisprudence islamique");
        await Assert.That(result["lookup.madhab.HANAFI.full_name"]).IsEqualTo("Hanafite");
    }

    [Test]
    public async Task Handle_WhenNoTranslations_ReturnsEmptyDictionary()
    {
        var provider = Substitute.For<ITranslationManagementProvider>();
        provider.ExportTranslationsAsync("zz", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<TranslationExport>>([]));

        var handler = new GetTranslationsQueryHandler(provider);

        var result = await handler.Handle(
            new GetTranslationsQuery { LanguageCode = "zz" },
            CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(0);
    }
}
