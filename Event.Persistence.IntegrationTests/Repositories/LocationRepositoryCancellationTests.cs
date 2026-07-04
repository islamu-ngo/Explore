// ABOUTME: PostgreSQL-backed cancellation tests for location repository custom reads.
// ABOUTME: Proves selected location repository queries forward caller cancellation into EF Core operations.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence.Repositories;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class LocationRepositoryCancellationTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task GetLocationsWithDetailsPaged_WhenCancellationRequested_ThrowsOperationCanceled()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new LocationRepository(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.GetLocationsWithDetailsPaged(
                pageNumber: 1,
                pageSize: 10,
                cancellation.Token));
    }

    [Test]
    public async Task ForgetPiiAsync_WhenCancellationRequested_ThrowsOperationCanceled()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new LocationRepository(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.ForgetPiiAsync(Guid.NewGuid(), cancellation.Token));
    }
}
