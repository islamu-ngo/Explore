// ABOUTME: PostgreSQL-backed cancellation tests for event-session language repository reads.
// ABOUTME: Proves selected session-language repository queries forward caller cancellation into EF Core operations.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence.Repositories;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventSessionLanguageRepositoryCancellationTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task GetLanguagesWithDetailsPaged_WhenCancellationRequested_ThrowsOperationCanceled()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new EventSessionLanguageRepository(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.GetLanguagesWithDetailsPaged(
                pageNumber: 1,
                pageSize: 10,
                cancellation.Token));
    }

    [Test]
    public async Task GetBySessionAndLanguage_WhenCancellationRequested_ThrowsOperationCanceled()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new EventSessionLanguageRepository(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.GetBySessionAndLanguage(
                Guid.NewGuid(),
                languageId: 1,
                cancellationToken: cancellation.Token));
    }
}
