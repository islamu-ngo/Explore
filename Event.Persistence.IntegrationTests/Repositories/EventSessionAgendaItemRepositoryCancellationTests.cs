// ABOUTME: PostgreSQL-backed cancellation tests for event-session agenda item repository reads.
// ABOUTME: Proves selected agenda item repository queries forward caller cancellation into EF Core operations.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence.Repositories;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventSessionAgendaItemRepositoryCancellationTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task GetAgendaItemsWithDetailsPaged_WhenCancellationRequested_ThrowsOperationCanceled()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new EventSessionAgendaItemRepository(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.GetAgendaItemsWithDetailsPaged(
                pageNumber: 1,
                pageSize: 10,
                cancellation.Token));
    }

    [Test]
    public async Task GetByIdWithDetails_WhenCancellationRequested_ThrowsOperationCanceled()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new EventSessionAgendaItemRepository(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.GetByIdWithDetails(Guid.NewGuid(), cancellation.Token));
    }
}
