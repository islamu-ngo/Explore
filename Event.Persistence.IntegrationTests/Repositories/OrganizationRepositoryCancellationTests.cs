// ABOUTME: PostgreSQL-backed cancellation tests for organization repository queries.
// ABOUTME: Proves organization repository read paths forward caller cancellation into EF Core operations.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence.Repositories;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class OrganizationRepositoryCancellationTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task GetOrganizationsWithDetailsPaged_WhenCancellationRequested_ThrowsOperationCanceled()
    {
        await using var context = fixture.CreateDbContext();
        var repository = new OrganizationRepository(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.GetOrganizationsWithDetailsPaged(
                pageNumber: 1,
                pageSize: 10,
                cancellation.Token));
    }
}
