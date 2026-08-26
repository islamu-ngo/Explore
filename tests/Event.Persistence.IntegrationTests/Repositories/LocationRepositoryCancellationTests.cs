// ABOUTME: PostgreSQL-backed cancellation tests for location repository custom reads.
// ABOUTME: Proves selected location repository queries forward caller cancellation into EF Core operations.

using System.Data.Common;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class LocationRepositoryCancellationTests(PostgreSqlContainerFixture fixture)
{
    private static readonly Guid TenantId = Guid.Parse("019b0000-0032-7000-8000-000000000001");
    private static readonly Guid LocationId = Guid.Parse("019b0000-0032-7000-8000-000000000002");
    private static readonly Guid ActorId = Guid.Parse("019b0000-0032-7000-8000-000000000003");

    [Test]
    public async Task Create_WhenCancelledAfterLocationInsertStarts_RollsBackLocationAndPii()
    {
        await fixture.ResetAsync();
        await SeedTenantAsync();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var interceptor = new BlockingLocationWriteInterceptor(LocationWriteKind.Insert, entered, release);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        await using ExploreDbContext context = fixture.CreateDbContext(interceptor);
        var repository = new LocationRepository(context);
        Location location = NewLocation();

        Task<Location> operation = repository.Create(location, cancellation.Token);
        await entered.Task.WaitAsync(timeout.Token);
        cancellation.Cancel();
        release.TrySetResult();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await operation.WaitAsync(timeout.Token));
        await using ExploreDbContext verification = fixture.CreateDbContext();
        await Assert.That(await verification.Locations.CountAsync(timeout.Token)).IsEqualTo(0);
        await Assert.That(await verification.LocationPii.CountAsync(timeout.Token)).IsEqualTo(0);
    }

    [Test]
    public async Task Update_WhenCancelledAfterGovernedLocationUpdateStarts_LeavesEntireDurableSnapshotUnchanged()
    {
        await fixture.ResetAsync();
        await SeedTenantAsync();
        LocationSnapshot before = await SeedLocationAsync();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var interceptor = new BlockingLocationWriteInterceptor(LocationWriteKind.Update, entered, release);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        await using ExploreDbContext context = fixture.CreateDbContext(interceptor);
        var repository = new LocationRepository(context);
        Location location = await repository.GetById(LocationId, timeout.Token)
            ?? throw new InvalidOperationException("The governed cancellation fixture was not found.");
        location.SetManualAddress("Replacement address", "2000");
        location.ApplyAddressGovernanceWithAudit(
            ActorId,
            LocationAddressSourceEnum.Manual,
            LocationAddressVisibilityEnum.CreatorPrivate,
            null,
            new DateTime(2026, 8, 26, 16, 0, 0, DateTimeKind.Utc));

        Task operation = repository.Update(location, cancellation.Token);
        await entered.Task.WaitAsync(timeout.Token);
        cancellation.Cancel();
        release.TrySetResult();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await operation.WaitAsync(timeout.Token));
        await AssertDurableSnapshotAsync(before, timeout.Token);
    }

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

    private async Task SeedTenantAsync()
    {
        await using ExploreDbContext context = fixture.CreateDbContext();
        TenantStatus status = await context.TenantStatuses
            .SingleAsync(item => item.Id == (int)TenantStatusEnum.Active);
        context.Tenants.Add(new Tenant
        {
            Id = TenantId,
            FullName = "Location cancellation tenant",
            Slug = "location-cancellation-tenant",
            TenantStatusId = status.Id,
            TenantStatus = status,
            CreatedAt = DateTime.UnixEpoch
        });
        await context.SaveChangesAsync();
    }

    private async Task<LocationSnapshot> SeedLocationAsync()
    {
        await using ExploreDbContext context = fixture.CreateDbContext();
        Location location = NewLocation();
        context.Locations.Add(location);
        await context.SaveChangesAsync();
        return Snapshot(location);
    }

    private static Location NewLocation()
    {
        var location = new Location
        {
            Id = LocationId,
            TenantId = TenantId,
            FullName = "Cancellation venue",
            Country = "BE",
            City = "Brussels",
            CreatedAt = DateTime.UnixEpoch,
            ConcurrencyStamp = Guid.Parse("019b0000-0032-7000-8000-000000000004")
        };
        location.SetProviderAddress(
            "Original address",
            "1000",
            GeoCoordinate.Create(50.8503, 4.3517));
        location.ApplyAddressGovernanceWithAudit(
            ActorId,
            LocationAddressSourceEnum.ProviderSelection,
            LocationAddressVisibilityEnum.CreatorPrivate,
            null,
            new DateTime(2026, 8, 26, 15, 0, 0, DateTimeKind.Utc));
        return location;
    }

    private async Task AssertDurableSnapshotAsync(LocationSnapshot expected, CancellationToken cancellationToken)
    {
        await using ExploreDbContext verification = fixture.CreateDbContext();
        Location durable = await verification.Locations
            .AsNoTracking()
            .Include(location => location.Pii)
            .SingleAsync(location => location.Id == LocationId, cancellationToken);
        await Assert.That(Snapshot(durable)).IsEqualTo(expected);
    }

    private static LocationSnapshot Snapshot(Location location) => new(
        location.Address,
        location.Postcode,
        location.Pii?.Latitude,
        location.Pii?.Longitude,
        location.AddressVisibility,
        location.AddressSource,
        location.AddressOrganizationId,
        location.CreatedAt,
        location.CreatedBy,
        location.UpdatedAt,
        location.UpdatedBy,
        location.ConcurrencyStamp);

    private sealed record LocationSnapshot(
        string? Address,
        string? Postcode,
        double? Latitude,
        double? Longitude,
        LocationAddressVisibilityEnum Visibility,
        LocationAddressSourceEnum Source,
        Guid? AddressOrganizationId,
        DateTime CreatedAt,
        Guid? CreatedBy,
        DateTime? UpdatedAt,
        Guid? UpdatedBy,
        Guid ConcurrencyStamp);

    private enum LocationWriteKind
    {
        Insert,
        Update
    }

    private sealed class BlockingLocationWriteInterceptor(
        LocationWriteKind writeKind,
        TaskCompletionSource entered,
        TaskCompletionSource release) : DbCommandInterceptor
    {
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (Matches(command.CommandText))
            {
                entered.TrySetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }

            return result;
        }

        private bool Matches(string commandText)
        {
            string verb = writeKind == LocationWriteKind.Insert ? "INSERT" : "UPDATE";
            return commandText.TrimStart().StartsWith(verb, StringComparison.OrdinalIgnoreCase)
                && commandText.Contains("locations", StringComparison.OrdinalIgnoreCase);
        }
    }
}
