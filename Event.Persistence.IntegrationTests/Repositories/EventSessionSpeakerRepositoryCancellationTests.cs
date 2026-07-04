// ABOUTME: PostgreSQL-backed cancellation tests for event-session speaker repository reads.
// ABOUTME: Proves selected session-speaker repository queries forward caller cancellation into EF Core operations.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence.Repositories;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventSessionSpeakerRepositoryCancellationTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task GetSpeakersWithDetailsPaged_WhenCancellationRequested_ThrowsOperationCanceled()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new EventSessionSpeakerRepository(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.GetSpeakersWithDetailsPaged(
                pageNumber: 1,
                pageSize: 10,
                cancellation.Token));
    }

    [Test]
    public async Task GetBySessionAndActor_WhenCancellationRequested_ThrowsOperationCanceled()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new EventSessionSpeakerRepository(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.GetBySessionAndActor(
                Guid.NewGuid(),
                Guid.NewGuid(),
                cancellationToken: cancellation.Token));
    }
}
