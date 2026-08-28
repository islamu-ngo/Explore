// ABOUTME: Defines RED PostgreSQL contracts for fair-return supply, waitlist order, and replacement payment.
// ABOUTME: Pins commercial equivalence, one-winner fences, crash replay, refund ordering, and PII minimization.

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Waitlist;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Persistence.IntegrationTests;

[ClassDataSource<PostgreSqlContainerFixture>(
    Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class FairReturnWaitlistConcurrencyTests(
    PostgreSqlContainerFixture fixture)
{
    private const string RepositoryTypeName =
        "Explore.Persistence.Repositories." +
        "FairReturnWaitlistRepository";
    private static readonly string RepositoryRoot =
        FindRepositoryRoot();
    private static readonly DateTime UtcNow =
        new(
            2026,
            8,
            28,
            12,
            0,
            0,
            DateTimeKind.Utc);

    [Test]
    public async Task AggregateFamilyOwnsSupplyQueueOfferBindingAndObservation()
    {
        string[] requiredTypes =
        [
            "Explore.Domain.FairReturnSupplyPolicy",
            "Explore.Domain.FairReturnSupplyUnit",
            "Explore.Domain.EventWaitlistEntry",
            "Explore.Domain.EventWaitlistOffer",
            "Explore.Domain.FairReturnSourceBinding",
            "Explore.Domain.WaitlistProviderObservation",
            "Explore.Domain.WaitlistRefundIntent",
        ];

        foreach (string typeName in requiredTypes)
        {
            await Assert.That(DomainType(typeName))
                .IsNotNull();
        }
    }

    [Test]
    public async Task LiteralQueueOrderIsPriorityThenEnqueueTimeThenStableId()
    {
        Type? repository =
            PersistenceType(RepositoryTypeName);
        await Assert.That(repository).IsNotNull();

        FieldInfo? order = repository!.GetField(
            "LiteralQueueOrder",
            BindingFlags.Public
            | BindingFlags.Static);
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.GetRawConstantValue())
            .IsEqualTo(
                "priority>enqueued-at>id");

        string source = await ReadSourceAsync(
            "src/Explore.Persistence/Repositories/" +
            "FairReturnWaitlistRepository.cs");
        await Assert.That(source).Contains(
            "OrderByDescending");
        await Assert.That(source).Contains(
            "Priority");
        await Assert.That(source).Contains(
            "ThenBy");
        await Assert.That(source).Contains(
            "EnqueuedAt");
        await Assert.That(source).Contains(
            "ThenBy");
        await Assert.That(source).Contains(
            "Id");
    }

    [Test]
    public async Task CommercialEquivalencePinsEveryOriginalSaleDimension()
    {
        Type? supply =
            DomainType(
                "Explore.Domain.FairReturnSupplyUnit");
        await Assert.That(supply).IsNotNull();

        string[] requiredProperties =
        [
            "TenantId",
            "EventId",
            "EventTicketTypeId",
            "TicketCatalogVersionId",
            "PurchasePolicySnapshotId",
            "CurrencyCode",
            "CommercialTermsDigest",
            "AdmissionEntitlementDigest",
            "GrossMinorUnits",
            "RefundFundingModeId",
        ];
        foreach (string property in
                 requiredProperties)
        {
            await Assert.That(supply!.GetProperty(
                    property))
                .IsNotNull();
        }

        await Assert.That(supply!.GetMethod(
                "IsCommerciallyEquivalentTo"))
            .IsNotNull();
    }

    [Test]
    public async Task SourceSubstitutionCannotMutateBuyerSnapshots()
    {
        Type? binding =
            DomainType(
                "Explore.Domain.FairReturnSourceBinding");
        await Assert.That(binding).IsNotNull();

        string[] immutableBuyerProperties =
        [
            "BuyerRegistrationOrderId",
            "BuyerRegistrationOrderLineId",
            "BuyerAccountUserId",
            "UnitAmountMinor",
            "CurrencyCode",
            "CommercialTermsDigest",
            "AdmissionEntitlementDigest",
        ];
        foreach (string property in
                 immutableBuyerProperties)
        {
            PropertyInfo? value =
                binding!.GetProperty(property);
            await Assert.That(value).IsNotNull();
            await Assert.That(
                    value!.SetMethod?.IsPublic)
                .IsFalse();
        }
        await Assert.That(binding!.GetMethod(
                "SubstituteSource"))
            .IsNotNull();
    }

    [Test]
    public async Task TenantFiltersAndUniqueWinnerIndexesAreModeled()
    {
        await using ExploreDbContext context =
            fixture.CreateDbContext();
        IModel model = context
            .GetService<IDesignTimeModel>()
            .Model;
        (string Entity, string[] UniqueSlot)[]
            expectations =
        [
            ("FairReturnSupplyUnit",
                ["TenantId",
                    "SellerRegistrationOrderLineId"]),
            ("EventWaitlistEntry",
                ["TenantId",
                    "OpenRegistrationOrderLineId"]),
            ("EventWaitlistOffer",
                ["TenantId",
                    "OpenEventWaitlistEntryId"]),
            ("FairReturnSourceBinding",
                ["TenantId",
                    "BuyerRegistrationOrderLineId"]),
            ("WaitlistProviderObservation",
                ["TenantId",
                    "ProviderCode",
                    "ProviderObjectType",
                    "ProviderObjectIdDigest"]),
            ("WaitlistRefundIntent",
                ["TenantId",
                    "FairReturnSourceBindingId"]),
        ];

        foreach ((string entityName,
                     string[] uniqueSlot) in
                 expectations)
        {
            IEntityType? entity = model
                .GetEntityTypes()
                .SingleOrDefault(value =>
                    value.ClrType.Name == entityName);
            await Assert.That(entity).IsNotNull();
            await Assert.That(
                    entity!.FindDeclaredQueryFilter(
                        QueryFilterNames.Tenant))
                .IsNotNull();
            await Assert.That(entity.GetIndexes()
                    .Any(index =>
                        index.IsUnique
                        && index.Properties
                            .Select(property =>
                                property.Name)
                            .SequenceEqual(uniqueSlot)))
                .IsTrue();
        }
    }

    [Test]
    public async Task CanonicalFenceOrdersSupplyQueueOfferBindingPaymentRefund()
    {
        Type? repository =
            PersistenceType(RepositoryTypeName);
        await Assert.That(repository).IsNotNull();
        FieldInfo? order = repository!.GetField(
            "CanonicalFenceOrder",
            BindingFlags.Public
            | BindingFlags.Static);
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.GetRawConstantValue())
            .IsEqualTo(
                "policy>supply>queue>offer>binding>" +
                "payment-dispatch>provider-observation>" +
                "refund-intent");

        string source = await ReadSourceAsync(
            "src/Explore.Persistence/Repositories/" +
            "FairReturnWaitlistRepository.cs");
        await Assert.That(source).Contains(
            "RelationalEntityRowFence");
        string rowFence = await ReadSourceAsync(
            "src/Explore.Persistence/Database/ProviderPrimitives/" +
            "RelationalEntityRowFence.cs");
        await Assert.That(rowFence).Contains(
            "FOR UPDATE");
    }

    [Test]
    public async Task AllocateWithdrawSubstituteExpireFinalizeAreAtomicPrimitives()
    {
        Type? repository =
            PersistenceType(RepositoryTypeName);
        await Assert.That(repository).IsNotNull();
        string[] methods =
        [
            "AllocateAsync",
            "WithdrawAsync",
            "SubstituteAsync",
            "ExpireOfferAsync",
            "FinalizeReplacementAsync",
        ];
        foreach (string method in methods)
        {
            await Assert.That(repository!.GetMethod(
                    method))
                .IsNotNull();
        }
    }

    [Test]
    public async Task PaymentHandoffMakesUnsafeWithdrawalPrivatelyConflicted()
    {
        Type? outcome = DomainType(
            "Explore.Domain.FairReturnOutcome");
        await Assert.That(outcome).IsNotNull();
        string[] names = Enum.GetNames(outcome!);
        await Assert.That(names).Contains(
            "SourceSubstituted");
        await Assert.That(names).Contains(
            "PaymentHandoffWon");
        await Assert.That(names).Contains(
            "PrivateConflict");
        await Assert.That(names).Contains(
            "NoCommerciallyEquivalentSupply");
        await Assert.That(names).Contains(
            "StaleObservation");
    }

    [Test]
    public async Task ReplacementSettlementCreatesOneRefundIntentAfterPayment()
    {
        Type? refundIntent = DomainType(
            "Explore.Domain.WaitlistRefundIntent");
        await Assert.That(refundIntent).IsNotNull();
        await Assert.That(refundIntent!.GetProperty(
                "ReplacementPaymentSettledAt"))
            .IsNotNull();
        await Assert.That(refundIntent.GetProperty(
                "OriginalPaymentAllocationId"))
            .IsNotNull();
        await Assert.That(refundIntent.GetProperty(
                "OutboxMessageId"))
            .IsNotNull();
        await Assert.That(refundIntent.GetProperties()
                .Select(property => property.Name))
            .DoesNotContain("Payload");
    }

    [Test]
    public async Task ProviderReplayUsesStableKeysAndMonotonicObservations()
    {
        Type? observation = DomainType(
            "Explore.Domain.WaitlistProviderObservation");
        await Assert.That(observation).IsNotNull();
        string[] required =
        [
            "ProviderCode",
            "ProviderObjectType",
            "ProviderObjectIdDigest",
            "ProviderObservationIdDigest",
            "ObservedAt",
            "StateCode",
        ];
        foreach (string property in required)
        {
            await Assert.That(observation!.GetProperty(
                    property))
                .IsNotNull();
        }
        await Assert.That(observation!.GetMethod(
                "ApplyIfNewer"))
            .IsNotNull();
    }

    [Test]
    public async Task CrashAndRaceTestsUseExactSignalsWithoutTimingLuck()
    {
        string source = await ReadSourceAsync(
            "tests/Event.Persistence.IntegrationTests/" +
            "FairReturnWaitlistConcurrencyTests.cs");
        string repository = await ReadSourceAsync(
            "src/Explore.Persistence/Repositories/" +
            "FairReturnWaitlistRepository.cs");

        await Assert.That(source).Contains(
            "TaskCompletionSource");
        await Assert.That(repository).DoesNotContain(
            "Task.Delay");
        await Assert.That(repository).DoesNotContain(
            "Thread.Sleep");
        await Assert.That(source).Contains(
            "AllArrived");
        await Assert.That(source).Contains(
            "Release");
        await Assert.That(repository).Contains(
            "SaveChangesAsync");
    }

    [Test]
    public async Task ConcurrentAllocationHasExactlyOneSupplyWinner()
    {
        const int contenderCount = 50;
        WaitlistSeed seed =
            await SeedWaitlistAsync(
                [
                    EntrySeed(
                        priority: 10,
                        UtcNow,
                        Guid.CreateVersion7()),
                    EntrySeed(
                        priority: 5,
                        UtcNow.AddMinutes(-1),
                        Guid.CreateVersion7()),
                ]);
        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(60));
        var gate = new WaitlistRaceGate(
            contenderCount);

        async Task<FairReturnWaitlistResult>
            AllocateAsync()
        {
            await using ExploreDbContext context =
                fixture.CreateDbContext();
            await gate.ArriveAsync(timeout.Token);
            return await new FairReturnWaitlistRepository(
                    context)
                .AllocateAsync(
                    new FairReturnAllocationRequest(
                        seed.TenantId,
                        seed.EventId,
                        seed.PolicyId,
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        UtcNow.AddMinutes(1)),
                    timeout.Token);
        }

        Task<FairReturnWaitlistResult>[] contenders =
            Enumerable.Range(0, contenderCount)
                .Select(_ => AllocateAsync())
                .ToArray();
        await gate.AllArrived.WaitAsync(timeout.Token);
        gate.Release();
        FairReturnWaitlistResult[] results =
            await Task.WhenAll(contenders);

        await using ExploreDbContext verification =
            fixture.CreateDbContext();
        await Assert.That(results.Count(value =>
                value.Outcome ==
                FairReturnOutcome.Allocated))
            .IsEqualTo(1);
        await Assert.That(
                await verification
                    .EventWaitlistOffers.CountAsync(
                        timeout.Token))
            .IsEqualTo(1);
        await Assert.That(
                await verification
                    .FairReturnSourceBindings.CountAsync(
                        timeout.Token))
            .IsEqualTo(1);
        await Assert.That(
                await verification
                    .FairReturnSupplyUnits.CountAsync(
                        value =>
                            value.StatusId ==
                            (int)FairReturnSupplyStatus
                                .Bound,
                        timeout.Token))
            .IsEqualTo(1);
    }

    [Test]
    public async Task ConcurrentAllocationAndLeaveHaveOneAuthorityWithoutDeadlock()
    {
        WaitlistSeed seed =
            await SeedWaitlistAsync(
                [
                    EntrySeed(
                        priority: 10,
                        UtcNow,
                        Guid.CreateVersion7()),
                ]);
        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(30));
        var gate = new WaitlistRaceGate(2);

        async Task<FairReturnWaitlistResult>
            AllocateAsync()
        {
            await using ExploreDbContext context =
                fixture.CreateDbContext();
            await gate.ArriveAsync(timeout.Token);
            return await new FairReturnWaitlistRepository(
                    context)
                .AllocateAsync(
                    new FairReturnAllocationRequest(
                        seed.TenantId,
                        seed.EventId,
                        seed.PolicyId,
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        UtcNow.AddMinutes(1)),
                    timeout.Token);
        }

        async Task<EventWaitlistEntry?> LeaveAsync()
        {
            await using ExploreDbContext context =
                fixture.CreateDbContext();
            await gate.ArriveAsync(timeout.Token);
            return await new FairReturnWaitlistRepository(
                    context)
                .LeaveAsync(
                    seed.TenantId,
                    seed.EventId,
                    seed.RegistrationOrderLineIds[0],
                    UtcNow.AddMinutes(1),
                    timeout.Token);
        }

        Task<FairReturnWaitlistResult> allocation =
            AllocateAsync();
        Task<EventWaitlistEntry?> leave = LeaveAsync();
        await gate.AllArrived.WaitAsync(timeout.Token);
        gate.Release();
        await Task.WhenAll(allocation, leave);

        await using ExploreDbContext verification =
            fixture.CreateDbContext();
        EventWaitlistEntry persisted =
            await verification.EventWaitlistEntries
                .AsNoTracking()
                .SingleAsync(timeout.Token);
        int offerCount =
            await verification.EventWaitlistOffers
                .CountAsync(timeout.Token);
        int boundSupplyCount =
            await verification.FairReturnSupplyUnits
                .CountAsync(
                    value =>
                        value.StatusId ==
                        (int)FairReturnSupplyStatus
                            .Bound,
                    timeout.Token);
        if (persisted.StatusId ==
            (int)EventWaitlistEntryStatus.Withdrawn)
        {
            await Assert.That(offerCount).IsEqualTo(0);
            await Assert.That(boundSupplyCount)
                .IsEqualTo(0);
        }
        else
        {
            await Assert.That(persisted.StatusId)
                .IsEqualTo(
                    (int)EventWaitlistEntryStatus.Offered);
            await Assert.That(offerCount).IsEqualTo(1);
            await Assert.That(boundSupplyCount)
                .IsEqualTo(1);
        }
    }

    [Test]
    public async Task QueuePositionExcludesCommerciallyDifferentEntries()
    {
        WaitlistAccessSeed seed =
            await SeedWaitlistAccessAsync();
        await using ExploreDbContext context =
            fixture.CreateDbContext();

        FairReturnWaitlistAccessContext? access =
            await new FairReturnWaitlistRepository(
                    context)
                .GetAccessAsync(
                    seed.TenantId,
                    seed.EventId,
                    seed.RegistrationOrderId,
                    seed.RegistrationOrderLineId,
                    CancellationToken.None);

        await Assert.That(access).IsNotNull();
        await Assert.That(access!.Position)
            .IsEqualTo(1);
    }

    [Test]
    public async Task AllocationUsesLiteralPriorityTimeAndStableIdOrder()
    {
        Guid stableFirst = Guid.Parse(
            "019c00aa-0000-7000-8000-000000000001");
        Guid stableSecond = Guid.Parse(
            "019c00aa-0000-7000-8000-000000000002");
        WaitlistSeed seed =
            await SeedWaitlistAsync(
                [
                    EntrySeed(
                        priority: 9,
                        UtcNow.AddMinutes(-10),
                        Guid.CreateVersion7()),
                    EntrySeed(
                        priority: 10,
                        UtcNow.AddMinutes(1),
                        stableSecond),
                    EntrySeed(
                        priority: 10,
                        UtcNow.AddMinutes(1),
                        stableFirst),
                ]);

        await using ExploreDbContext context =
            fixture.CreateDbContext();
        FairReturnWaitlistResult result =
            await new FairReturnWaitlistRepository(
                    context)
                .AllocateAsync(
                    new FairReturnAllocationRequest(
                        seed.TenantId,
                        seed.EventId,
                        seed.PolicyId,
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        UtcNow.AddMinutes(2)),
                    CancellationToken.None);

        await Assert.That(result.Outcome)
            .IsEqualTo(FairReturnOutcome.Allocated);
        await Assert.That(result.Entry?.Id)
            .IsEqualTo(stableFirst);
    }

    [Test]
    public async Task WithdrawalAtomicallySubstitutesEquivalentSupply()
    {
        WaitlistSeed seed =
            await SeedWaitlistAsync(
                [
                    EntrySeed(
                        priority: 1,
                        UtcNow,
                        Guid.CreateVersion7()),
                ],
                supplyCount: 2);
        FairReturnWaitlistResult allocation =
            await AllocateSeedAsync(seed);
        FairReturnSourceBinding original =
            allocation.Binding!;

        await using ExploreDbContext context =
            fixture.CreateDbContext();
        FairReturnWaitlistResult withdrawal =
            await new FairReturnWaitlistRepository(
                    context)
                .WithdrawAsync(
                    new FairReturnWithdrawalRequest(
                        seed.TenantId,
                        seed.EventId,
                        allocation.Supply!.Id,
                        UtcNow.AddMinutes(2)),
                    CancellationToken.None);

        await Assert.That(withdrawal.Outcome)
            .IsEqualTo(
                FairReturnOutcome.SourceSubstituted);
        await Assert.That(
                withdrawal.Binding?
                    .FairReturnSupplyUnitId)
            .IsNotEqualTo(
                allocation.Supply!.Id);
        await Assert.That(
                withdrawal.Binding?
                    .BuyerRegistrationOrderId)
            .IsEqualTo(
                original.BuyerRegistrationOrderId);
        await Assert.That(
                withdrawal.Binding?
                    .BuyerRegistrationOrderLineId)
            .IsEqualTo(
                original.BuyerRegistrationOrderLineId);
        await Assert.That(
                withdrawal.Binding?.UnitAmountMinor)
            .IsEqualTo(original.UnitAmountMinor);
        await Assert.That(
                withdrawal.Binding?.CurrencyCode)
            .IsEqualTo(original.CurrencyCode);
        await Assert.That(
                withdrawal.Binding?
                    .CommercialTermsDigest)
            .IsEqualTo(
                original.CommercialTermsDigest);
        await Assert.That(
                withdrawal.Binding?
                    .AdmissionEntitlementDigest)
            .IsEqualTo(
                original.AdmissionEntitlementDigest);
    }

    [Test]
    public async Task WithdrawalWithoutEquivalentSupplyKeepsSaleBound()
    {
        WaitlistSeed seed =
            await SeedWaitlistAsync(
                [
                    EntrySeed(
                        priority: 1,
                        UtcNow,
                        Guid.CreateVersion7()),
                ]);
        FairReturnWaitlistResult allocation =
            await AllocateSeedAsync(seed);

        await using ExploreDbContext context =
            fixture.CreateDbContext();
        FairReturnWaitlistResult withdrawal =
            await new FairReturnWaitlistRepository(
                    context)
                .WithdrawAsync(
                    new FairReturnWithdrawalRequest(
                        seed.TenantId,
                        seed.EventId,
                        allocation.Supply!.Id,
                        UtcNow.AddMinutes(2)),
                    CancellationToken.None);

        await Assert.That(withdrawal.Outcome)
            .IsEqualTo(FairReturnOutcome.PrivateConflict);
        await using ExploreDbContext verification =
            fixture.CreateDbContext();
        FairReturnSupplyUnit persisted =
            await verification.FairReturnSupplyUnits
                .AsNoTracking()
                .SingleAsync(value =>
                    value.Id == allocation.Supply.Id);
        await Assert.That(persisted.StatusId)
            .IsEqualTo(
                (int)FairReturnSupplyStatus.Bound);
        await Assert.That(persisted.WithdrawnAt)
            .IsNull();
    }

    [Test]
    public async Task ConcurrentWithdrawAndSubstituteDoNotDeadlockOrDoubleBind()
    {
        WaitlistSeed seed =
            await SeedWaitlistAsync(
                [
                    EntrySeed(
                        priority: 1,
                        UtcNow,
                        Guid.CreateVersion7()),
                ],
                supplyCount: 2);
        FairReturnWaitlistResult allocation =
            await AllocateSeedAsync(seed);
        Guid replacementId = seed.SupplyIds
            .Single(value =>
                value != allocation.Supply!.Id);
        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(30));
        var gate = new WaitlistRaceGate(2);

        async Task<FairReturnWaitlistResult>
            WithdrawAsync()
        {
            await using ExploreDbContext context =
                fixture.CreateDbContext();
            await gate.ArriveAsync(timeout.Token);
            return await new FairReturnWaitlistRepository(
                    context)
                .WithdrawAsync(
                    new FairReturnWithdrawalRequest(
                        seed.TenantId,
                        seed.EventId,
                        allocation.Supply!.Id,
                        UtcNow.AddMinutes(2)),
                    timeout.Token);
        }

        async Task<FairReturnWaitlistResult>
            SubstituteAsync()
        {
            await using ExploreDbContext context =
                fixture.CreateDbContext();
            await gate.ArriveAsync(timeout.Token);
            return await new FairReturnWaitlistRepository(
                    context)
                .SubstituteAsync(
                    new FairReturnSubstitutionRequest(
                        seed.TenantId,
                        seed.EventId,
                        allocation.Binding!.Id,
                        replacementId,
                        UtcNow.AddMinutes(2)),
                    timeout.Token);
        }

        Task<FairReturnWaitlistResult> withdrawal =
            WithdrawAsync();
        Task<FairReturnWaitlistResult> substitution =
            SubstituteAsync();
        await gate.AllArrived.WaitAsync(timeout.Token);
        gate.Release();
        FairReturnWaitlistResult[] results =
            await Task.WhenAll(
                withdrawal,
                substitution);

        await Assert.That(results.Any(value =>
                value.Outcome ==
                FairReturnOutcome.SourceSubstituted))
            .IsTrue();
        await using ExploreDbContext verification =
            fixture.CreateDbContext();
        await Assert.That(
                await verification
                    .FairReturnSupplyUnits.CountAsync(
                        value =>
                            value.StatusId ==
                            (int)FairReturnSupplyStatus
                                .Bound,
                        timeout.Token))
            .IsEqualTo(1);
        await Assert.That(
                await verification
                    .FairReturnSourceBindings.CountAsync(
                        timeout.Token))
            .IsEqualTo(1);
    }

    [Test]
    public async Task ExpiryAndFinalizeAtBoundaryHaveOneTerminalWinner()
    {
        WaitlistSeed seed =
            await SeedWaitlistAsync(
                [
                    EntrySeed(
                        priority: 1,
                        UtcNow,
                        Guid.CreateVersion7()),
                ]);
        FairReturnWaitlistResult allocation =
            await AllocateSeedAsync(seed);
        DateTime boundary = allocation.Offer!.ExpiresAt;
        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(30));
        var gate = new WaitlistRaceGate(2);

        async Task<FairReturnWaitlistResult>
            ExpireAsync()
        {
            await using ExploreDbContext context =
                fixture.CreateDbContext();
            await gate.ArriveAsync(timeout.Token);
            return await new FairReturnWaitlistRepository(
                    context)
                .ExpireOfferAsync(
                    new WaitlistOfferExpiryRequest(
                        seed.TenantId,
                        seed.EventId,
                        allocation.Offer.Id,
                        boundary),
                    timeout.Token);
        }

        async Task<FairReturnWaitlistResult>
            FinalizeAsync()
        {
            await using ExploreDbContext context =
                fixture.CreateDbContext();
            await gate.ArriveAsync(timeout.Token);
            return await new FairReturnWaitlistRepository(
                    context)
                .FinalizeReplacementAsync(
                    new WaitlistReplacementFinalizeRequest(
                        seed.TenantId,
                        seed.EventId,
                        allocation.Offer.Id,
                        boundary),
                    timeout.Token);
        }

        Task<FairReturnWaitlistResult> expiry =
            ExpireAsync();
        Task<FairReturnWaitlistResult> finalization =
            FinalizeAsync();
        await gate.AllArrived.WaitAsync(timeout.Token);
        gate.Release();
        FairReturnWaitlistResult[] results =
            await Task.WhenAll(expiry, finalization);

        await Assert.That(results.Count(value =>
                value.Outcome ==
                FairReturnOutcome.OfferExpired))
            .IsEqualTo(1);
        await Assert.That(results.Count(value =>
                value.Outcome ==
                FairReturnOutcome.AlreadyApplied))
            .IsEqualTo(1);
        await using ExploreDbContext verification =
            fixture.CreateDbContext();
        EventWaitlistOffer offer =
            await verification.EventWaitlistOffers
                .AsNoTracking()
                .SingleAsync(value =>
                    value.Id == allocation.Offer.Id,
                    timeout.Token);
        FairReturnSupplyUnit supply =
            await verification.FairReturnSupplyUnits
                .AsNoTracking()
                .SingleAsync(value =>
                    value.Id == allocation.Supply!.Id,
                    timeout.Token);
        await Assert.That(offer.StatusId)
            .IsEqualTo(
                (int)EventWaitlistOfferStatus.Expired);
        await Assert.That(supply.StatusId)
            .IsEqualTo(
                (int)FairReturnSupplyStatus.Available);
    }

    [Test]
    public async Task ProviderObservationReplayIsMonotonicAndContradictionSafe()
    {
        WaitlistSeed seed =
            await SeedWaitlistAsync(
                [
                    EntrySeed(
                        priority: 1,
                        UtcNow,
                        Guid.CreateVersion7()),
                ]);
        FairReturnWaitlistResult allocation =
            await AllocateSeedAsync(seed);
        WaitlistProviderObservation observation =
            WaitlistProviderObservation.Create(
                Guid.CreateVersion7(),
                allocation.Binding!,
                "provider",
                "payment",
                Digest("provider-object"),
                Digest("first-observation"),
                UtcNow.AddMinutes(3),
                "pending");

        FairReturnOutcome duplicate =
            observation.ApplyIfNewer(
                Digest("first-observation"),
                UtcNow.AddMinutes(3),
                "pending");
        FairReturnOutcome contradiction =
            observation.ApplyIfNewer(
                Digest("contradiction"),
                UtcNow.AddMinutes(3),
                "settled");
        FairReturnOutcome stale =
            observation.ApplyIfNewer(
                Digest("stale"),
                UtcNow.AddMinutes(2),
                "failed");
        FairReturnOutcome newer =
            observation.ApplyIfNewer(
                Digest("newer"),
                UtcNow.AddMinutes(4),
                "settled");

        await Assert.That(duplicate)
            .IsEqualTo(
                FairReturnOutcome.AlreadyApplied);
        await Assert.That(contradiction)
            .IsEqualTo(
                FairReturnOutcome.StaleObservation);
        await Assert.That(stale)
            .IsEqualTo(
                FairReturnOutcome.StaleObservation);
        await Assert.That(newer)
            .IsEqualTo(
                FairReturnOutcome
                    .ReplacementFinalized);
        await Assert.That(observation.StateCode)
            .IsEqualTo("SETTLED");
        await Assert.That(
                observation
                    .ProviderObservationIdDigest)
            .IsEqualTo(Digest("newer"));
    }

    [Test]
    public async Task CommercialEquivalenceRejectsEveryChangedSaleDimension()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid ticketTypeId = Guid.CreateVersion7();
        Guid catalogId = Guid.CreateVersion7();
        Guid policySnapshotId = Guid.CreateVersion7();
        FairReturnSupplyUnit baseline = Supply(
            tenantId,
            eventId,
            ticketTypeId,
            catalogId,
            policySnapshotId,
            "USD",
            Digest("terms"),
            Digest("entitlement"),
            12_345,
            refundFundingModeId: 1);
        FairReturnSupplyUnit[] changed =
        [
            Supply(Guid.CreateVersion7(), eventId,
                ticketTypeId, catalogId,
                policySnapshotId, "USD",
                Digest("terms"), Digest("entitlement"),
                12_345, 1),
            Supply(tenantId, Guid.CreateVersion7(),
                ticketTypeId, catalogId,
                policySnapshotId, "USD",
                Digest("terms"), Digest("entitlement"),
                12_345, 1),
            Supply(tenantId, eventId,
                Guid.CreateVersion7(), catalogId,
                policySnapshotId, "USD",
                Digest("terms"), Digest("entitlement"),
                12_345, 1),
            Supply(tenantId, eventId,
                ticketTypeId, Guid.CreateVersion7(),
                policySnapshotId, "USD",
                Digest("terms"), Digest("entitlement"),
                12_345, 1),
            Supply(tenantId, eventId,
                ticketTypeId, catalogId,
                Guid.CreateVersion7(), "USD",
                Digest("terms"), Digest("entitlement"),
                12_345, 1),
            Supply(tenantId, eventId,
                ticketTypeId, catalogId,
                policySnapshotId, "EUR",
                Digest("terms"), Digest("entitlement"),
                12_345, 1),
            Supply(tenantId, eventId,
                ticketTypeId, catalogId,
                policySnapshotId, "USD",
                Digest("changed"), Digest("entitlement"),
                12_345, 1),
            Supply(tenantId, eventId,
                ticketTypeId, catalogId,
                policySnapshotId, "USD",
                Digest("terms"), Digest("changed"),
                12_345, 1),
            Supply(tenantId, eventId,
                ticketTypeId, catalogId,
                policySnapshotId, "USD",
                Digest("terms"), Digest("entitlement"),
                12_346, 1),
            Supply(tenantId, eventId,
                ticketTypeId, catalogId,
                policySnapshotId, "USD",
                Digest("terms"), Digest("entitlement"),
                12_345, 2),
        ];

        foreach (FairReturnSupplyUnit candidate
                 in changed)
        {
            await Assert.That(
                    baseline.IsCommerciallyEquivalentTo(
                        candidate))
                .IsFalse();
        }
    }

    [Test]
    public async Task WaitlistStateStoresReferencesWithoutParticipantPii()
    {
        string[] typeNames =
        [
            "Explore.Domain.EventWaitlistEntry",
            "Explore.Domain.EventWaitlistOffer",
            "Explore.Domain.FairReturnSourceBinding",
        ];
        string[] forbidden =
        [
            "Email",
            "Phone",
            "Name",
            "Address",
            "Answer",
            "ConsentText",
            "PaymentInstrument",
            "ProviderPayload",
        ];

        foreach (string typeName in typeNames)
        {
            Type? type = DomainType(typeName);
            await Assert.That(type).IsNotNull();
            string[] properties = type!.GetProperties()
                .Select(property => property.Name)
                .ToArray();
            foreach (string property in forbidden)
            {
                await Assert.That(properties)
                    .DoesNotContain(property);
            }
        }
    }

    [Test]
    public async Task ConcurrentClaimsHaveOneWinnerAndExpiredLeaseReclaims()
    {
        OrchestrationSeed seed =
            await SeedOrchestrationAsync();

        async Task<IReadOnlyList<
            FairReturnOrchestrationClaim>> ClaimAsync(
                string owner,
                DateTime claimedAt)
        {
            await using ExploreDbContext context =
                fixture.CreateDbContext();
            return await new
                FairReturnOrchestrationRepository(
                    context)
                .TryClaimDueAsync(
                    claimedAt,
                    owner,
                    seed.EffectId,
                    1,
                    1,
                    TimeSpan.FromMinutes(2),
                    CancellationToken.None);
        }

        IReadOnlyList<
            FairReturnOrchestrationClaim>[] raced =
            await Task.WhenAll(
                ClaimAsync("claim-a", UtcNow),
                ClaimAsync("claim-b", UtcNow));
        FairReturnOrchestrationClaim winner =
            raced.SelectMany(value => value)
                .Single();

        IReadOnlyList<
            FairReturnOrchestrationClaim> reclaimed =
            await ClaimAsync(
                "claim-after-restart",
                UtcNow.AddMinutes(3));

        await Assert.That(reclaimed.Count)
            .IsEqualTo(1);
        await Assert.That(
                reclaimed[0].ExpiredLease)
            .IsTrue();
        await Assert.That(
                reclaimed[0].ProcessingFence)
            .IsEqualTo(
                winner.ProcessingFence + 1);
        await Assert.That(
                reclaimed[0].ProviderIdempotencyKey)
            .IsEqualTo(
                winner.ProviderIdempotencyKey);
        await Assert.That(
                reclaimed[0].StableOperationId)
            .IsEqualTo(
                winner.StableOperationId);
    }

    [Test]
    public async Task RefundIntentAndOutboxWaitForReplacementSettlement()
    {
        OrchestrationSeed seed =
            await SeedOrchestrationAsync();
        FairReturnOrchestrationClaim claim;
        await using (ExploreDbContext claimContext =
                     fixture.CreateDbContext())
        {
            claim = (await new
                    FairReturnOrchestrationRepository(
                        claimContext)
                    .TryClaimDueAsync(
                        UtcNow,
                        "settlement-fence",
                        seed.EffectId,
                        1,
                        1,
                        TimeSpan.FromMinutes(2),
                        CancellationToken.None))
                .Single();
        }

        await using (ExploreDbContext beforeContext =
                     fixture.CreateDbContext())
        {
            WaitlistRefundIntent? premature =
                await new
                    FairReturnOrchestrationRepository(
                        beforeContext)
                    .CreateRefundIntentAsync(
                        claim,
                        UtcNow.AddSeconds(1),
                        CancellationToken.None);
            await Assert.That(premature).IsNull();
            await Assert.That(
                    await beforeContext
                        .WaitlistRefundIntents
                        .CountAsync())
                .IsEqualTo(0);
            await Assert.That(
                    await beforeContext
                        .OutboxMessages
                        .CountAsync())
                .IsEqualTo(0);
        }

        await using (ExploreDbContext settleContext =
                     fixture.CreateDbContext())
        {
            var repository = new
                FairReturnOrchestrationRepository(
                    settleContext);
            await Assert.That(
                    await repository
                        .ObserveReplacementSettlementAsync(
                            claim,
                            UtcNow.AddSeconds(2),
                            CancellationToken.None))
                .IsTrue();
        }

        Guid firstRefundIntentId;
        await using (ExploreDbContext refundContext =
                     fixture.CreateDbContext())
        {
            var repository = new
                FairReturnOrchestrationRepository(
                    refundContext);
            WaitlistRefundIntent created =
                (await repository
                    .CreateRefundIntentAsync(
                        claim,
                        UtcNow.AddSeconds(2),
                        CancellationToken.None))!;
            firstRefundIntentId = created.Id;
        }

        await using ExploreDbContext replayContext =
            fixture.CreateDbContext();
        WaitlistRefundIntent replayed =
            (await new
                FairReturnOrchestrationRepository(
                    replayContext)
                .CreateRefundIntentAsync(
                    claim,
                    UtcNow.AddSeconds(2),
                    CancellationToken.None))!;
        await Assert.That(replayed.Id)
            .IsEqualTo(firstRefundIntentId);
        await Assert.That(
                await replayContext
                    .WaitlistRefundIntents
                    .CountAsync())
            .IsEqualTo(1);
        await Assert.That(
                await replayContext
                    .OutboxMessages
                    .CountAsync())
            .IsEqualTo(1);
    }

    private static Type? DomainType(
        string fullName) =>
        typeof(AdmissionTicket).Assembly.GetType(
            fullName);

    private static Type? PersistenceType(
        string fullName) =>
        typeof(ExploreDbContext).Assembly.GetType(
            fullName);

    private async Task<OrchestrationSeed>
        SeedOrchestrationAsync()
    {
        WaitlistSeed waitlist =
            await SeedWaitlistAsync(
                [
                    EntrySeed(
                        1,
                        UtcNow.AddMinutes(-1),
                        Guid.CreateVersion7()),
                ]);
        _ = await AllocateSeedAsync(waitlist);
        Guid bindingId;
        Guid originalPaymentId =
            Guid.CreateVersion7();
        Guid replacementPaymentId =
            Guid.CreateVersion7();

        await using (ExploreDbContext seedContext =
                     fixture.CreateDbContext())
        {
            FairReturnSourceBinding binding =
                await seedContext
                    .FairReturnSourceBindings
                    .SingleAsync(value =>
                        value.TenantId ==
                            waitlist.TenantId
                        && value.EventId ==
                            waitlist.EventId);
            bindingId = binding.Id;
            RegistrationOrder buyerOrder =
                RegistrationOrder.Create(
                    binding
                        .BuyerRegistrationOrderId,
                    waitlist.TenantId,
                    waitlist.EventId,
                    Guid.CreateVersion7(),
                    null,
                    BookingPartyTypeEnum.Individual,
                    Guid.CreateVersion7(),
                    RegistrationParticipationSnapshot
                        .Create(
                            Guid.CreateVersion7(),
                            1,
                            1,
                            1,
                            GuestRecoveryPolicyEnum
                                .VerifiedEmailRequired),
                    null,
                    null,
                    "EUR",
                    UtcNow,
                    UtcNow.AddMinutes(30));
            RegistrationOrder originalOrder =
                RegistrationOrder.Create(
                    Guid.CreateVersion7(),
                    waitlist.TenantId,
                    waitlist.EventId,
                    Guid.CreateVersion7(),
                    null,
                    BookingPartyTypeEnum.Individual,
                    Guid.CreateVersion7(),
                    RegistrationParticipationSnapshot
                        .Create(
                            Guid.CreateVersion7(),
                            1,
                            1,
                            1,
                            GuestRecoveryPolicyEnum
                                .VerifiedEmailRequired),
                    null,
                    null,
                    "EUR",
                    UtcNow,
                    UtcNow.AddMinutes(30));
            PaymentAttempt original =
                PaymentAttempt.Create(
                    originalPaymentId,
                    waitlist.TenantId,
                    originalOrder.Id,
                    PaymentRecipient(
                        waitlist.TenantId,
                        waitlist.EventId),
                    "OrganizerDirect",
                    "2026-08-20.acacia",
                    "fair-return",
                    Money.Create(1_000, "EUR"),
                    Money.Create(75, "EUR"),
                    Money.Create(0, "EUR"),
                    $"payment:{originalPaymentId:N}",
                    UtcNow,
                    UtcNow.AddMinutes(30));
            PaidOrderAcceptanceSnapshot acceptance =
                PaidAcceptanceTestFacts.Create(
                    waitlist.TenantId,
                    originalOrder.Id,
                    waitlist.EventId,
                    "fair-return",
                    Guid.CreateVersion7(),
                    1_000,
                    75,
                    0,
                    UtcNow);
            original.AttachAcceptance(acceptance);
            original.MarkSucceeded(
                $"pi_{originalPaymentId:N}",
                UtcNow.AddSeconds(1),
                "payment-settled");
            PaymentAttempt replacement =
                PaymentAttempt.Create(
                    replacementPaymentId,
                    waitlist.TenantId,
                    buyerOrder.Id,
                    PaymentRecipient(
                        waitlist.TenantId,
                        waitlist.EventId),
                    "OrganizerDirect",
                    "2026-08-20.acacia",
                    "fair-return",
                    Money.Create(1_000, "EUR"),
                    Money.Create(75, "EUR"),
                    Money.Create(0, "EUR"),
                    $"payment:{replacementPaymentId:N}",
                    UtcNow,
                    UtcNow.AddMinutes(30));

            await seedContext.Database
                .OpenConnectionAsync();
            await seedContext.Database
                .ExecuteSqlRawAsync(
                    "SET session_replication_role = " +
                    "replica;");
            seedContext.RegistrationOrders.AddRange(
                originalOrder,
                buyerOrder);
            seedContext.PaymentAttempts.AddRange(
                original,
                replacement);
            await seedContext.SaveChangesAsync();
            await seedContext.Database
                .ExecuteSqlRawAsync(
                    "SET session_replication_role = " +
                    "origin;");
        }

        Guid effectId = Guid.CreateVersion7();
        await using ExploreDbContext intentContext =
            fixture.CreateDbContext();
        FairReturnSourceBinding persistedBinding =
            await intentContext
                .FairReturnSourceBindings
                .SingleAsync(value =>
                    value.Id == bindingId);
        PaymentAttempt persistedOriginal =
            await intentContext.PaymentAttempts
                .Include(value =>
                    value.AcceptanceSnapshot)
                .SingleAsync(value =>
                    value.Id == originalPaymentId);
        RefundAttempt reservedRefund =
            RefundAttempt.Create(
                Guid.CreateVersion7(),
                waitlist.TenantId,
                originalPaymentId,
                persistedOriginal.AcceptanceSnapshot!,
                "acct_original",
                $"pi_{originalPaymentId:N}",
                $"fair-return:{bindingId:N}",
                1_000,
                UtcNow.AddSeconds(2));
        WaitlistPaymentIntent intent =
            WaitlistPaymentIntent.Create(
                Guid.CreateVersion7(),
                persistedBinding,
                reservedRefund,
                replacementPaymentId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                UtcNow);
        FairReturnOrchestrationEffect effect =
            FairReturnOrchestrationEffect.Create(
                effectId,
                intent,
                5,
                UtcNow);
        await new FairReturnOrchestrationRepository(
                intentContext)
            .CreatePaymentIntentAsync(
                intent,
                reservedRefund,
                effect,
                CancellationToken.None);
        return new OrchestrationSeed(
            waitlist.TenantId,
            effectId);
    }

    private static
        OrganizerPaymentRecipientSnapshot
        PaymentRecipient(
            Guid tenantId,
            Guid eventId) =>
        OrganizerPaymentRecipientSnapshot.Create(
            tenantId,
            eventId,
            Guid.CreateVersion7(),
            "stripe",
            "OrganizerDirect",
            "acct_original",
            "BE",
            "EUR",
            Guid.CreateVersion7(),
            null,
            UtcNow);

    private async Task<WaitlistSeed> SeedWaitlistAsync(
        IReadOnlyList<WaitlistEntrySeed> entries,
        int supplyCount = 1)
    {
        await fixture.ResetAsync();
        await using ExploreDbContext context =
            fixture.CreateDbContext();
        var tenant = new Tenant
        {
            FullName = "Fair return test",
            Slug =
                $"fair-return-{Guid.CreateVersion7():N}",
            TenantStatusId =
                (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        Guid eventId = Guid.CreateVersion7();
        Guid ticketTypeId = Guid.CreateVersion7();
        Guid catalogId = Guid.CreateVersion7();
        Guid purchasePolicySnapshotId =
            Guid.CreateVersion7();
        FairReturnSupplyPolicy policy =
            FairReturnSupplyPolicy.Create(
                Guid.CreateVersion7(),
                tenant.Id,
                eventId,
                catalogId,
                ticketTypeId,
                isEnabled: true,
                offerLifetimeMinutes: 60,
                UtcNow);
        FairReturnSupplyUnit[] supplies =
            Enumerable.Range(0, supplyCount)
                .Select(_ => Supply(
                    tenant.Id,
                    eventId,
                    ticketTypeId,
                    catalogId,
                    purchasePolicySnapshotId,
                    "USD",
                    Digest("terms"),
                    Digest("entitlement"),
                    12_345,
                    refundFundingModeId: 1))
                .ToArray();
        context.Add(policy);
        context.AddRange(supplies);
        var registrationOrderLineIds =
            new List<Guid>(entries.Count);
        foreach (WaitlistEntrySeed entry in entries)
        {
            Guid registrationOrderLineId =
                Guid.CreateVersion7();
            registrationOrderLineIds.Add(
                registrationOrderLineId);
            context.EventWaitlistEntries.Add(
                EventWaitlistEntry.Enqueue(
                    entry.Id,
                    tenant.Id,
                    eventId,
                    ticketTypeId,
                    catalogId,
                    purchasePolicySnapshotId,
                    Guid.CreateVersion7(),
                    registrationOrderLineId,
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    "USD",
                    Digest("terms"),
                    Digest("entitlement"),
                    12_345,
                    1,
                    entry.Priority,
                    entry.EnqueuedAtUtc));
        }
        await context.SaveChangesAsync();
        return new WaitlistSeed(
            tenant.Id,
            eventId,
            policy.Id,
            supplies.Select(value => value.Id)
                .ToArray(),
            registrationOrderLineIds);
    }

    private async Task<WaitlistAccessSeed>
        SeedWaitlistAccessAsync()
    {
        await fixture.ResetAsync();
        await using ExploreDbContext context =
            fixture.CreateDbContext();
        var tenant = new Tenant
        {
            FullName = "Waitlist position test",
            Slug =
                $"waitlist-position-" +
                $"{Guid.CreateVersion7():N}",
            TenantStatusId =
                (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
        var user = new User
        {
            Pii = new UserPii
            {
                Email =
                    $"waitlist-{Guid.CreateVersion7():N}" +
                    "@example.test",
                FirstName = "Queue",
                LastName = "Buyer",
            },
        };
        context.AddRange(tenant, user);
        await context.SaveChangesAsync();
        var actor = new Actor
        {
            Pii = new ActorPii
            {
                DisplayName = "Waitlist organizer",
            },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            UserId = user.Id,
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var eventEntity =
            new DomainEvent(EventStatusEnum.Draft)
            {
                Id = Guid.CreateVersion7(),
                Title = "Waitlist position event",
                Subtitle = string.Empty,
                Description = string.Empty,
                FirstSessionDate =
                    DateOnly.FromDateTime(
                        UtcNow.AddDays(2)),
                LastSessionDate =
                    DateOnly.FromDateTime(
                        UtcNow.AddDays(2)),
                EventTypeId = 1,
                AudienceGenderId = 1,
                AudienceAgeId = 1,
                ActorId = actor.Id,
                Actor = null!,
                OrganizerActorId = actor.Id,
                TenantId = tenant.Id,
                Tenant = tenant,
                VisibilityTypeId = 1,
                VisibilityType = null!,
                EventStatus = null!,
                EventFormatId = 1,
                EventFormat = null!,
                EventProvenanceTypeId =
                    (int)EventProvenanceTypeEnum
                        .OrganizerCreated,
            };
        EventTicketCatalogVersion catalog =
            EventTicketCatalogVersion.Create(
                tenant.Id,
                eventEntity.Id,
                "USD",
                1);
        EventTicketType ticketType =
            EventTicketType.Create(
                Guid.CreateVersion7(),
                tenant.Id,
                catalog.Id,
                "Waitlist ticket",
                "USD",
                TicketPricingModeEnum.Free,
                fixedPrice: null,
                minimumPrice: null,
                suggestedPrice: null,
                ParticipantDataCollectionModeEnum.None,
                capacityPoolId: null,
                minimumAge: null,
                maximumAge: null,
                requiresGuardian: false,
                requiresApproval: false,
                perOrderLimit: null,
                perAccountLimit: null,
                perVerifiedContactLimit: null,
                perBookingPartyLimit: null);
        catalog.AddTicketType(ticketType, null);
        catalog.AddEntitlement(
            ticketType,
            TicketTypeEntitlement.CreateForEvent(
                ticketType.Id,
                tenant.Id,
                eventEntity.Id,
                includedQuantity: 1));
        catalog.Publish();
        RegistrationOrder order =
            RegistrationOrder.Create(
                tenant.Id,
                eventEntity.Id,
                user.Id,
                actor.Id,
                BookingPartyTypeEnum.Individual,
                catalog.Id,
                RegistrationParticipationSnapshot
                    .Create(
                        Guid.CreateVersion7(),
                        1,
                        1,
                        1,
                        null),
                registrationWorkflowVersionId: null,
                guestAccessTokenHash: null,
                "USD",
                UtcNow,
                expiresAt: null);
        RegistrationOrderLine line =
            RegistrationOrderLine.Create(
                catalog,
                ticketType,
                order.Id,
                quantity: 1,
                chosenUnitPriceAmount: null,
                platformFeePolicy: null);
        order.AddLine(line);
        Guid purchasePolicySnapshotId =
            Guid.CreateVersion7();
        FairReturnSupplyPolicy policy =
            FairReturnSupplyPolicy.Create(
                Guid.CreateVersion7(),
                tenant.Id,
                eventEntity.Id,
                catalog.Id,
                ticketType.Id,
                isEnabled: true,
                offerLifetimeMinutes: 60,
                UtcNow);
        string terms = Digest("terms");
        string entitlement = Digest("entitlement");
        EventWaitlistEntry Target(
            Guid registrationOrderId,
            Guid registrationOrderLineId,
            Guid policySnapshotId,
            string currency,
            string commercialDigest,
            string entitlementDigest,
            long gross,
            int funding,
            int priority) =>
            EventWaitlistEntry.Enqueue(
                Guid.CreateVersion7(),
                tenant.Id,
                eventEntity.Id,
                ticketType.Id,
                catalog.Id,
                policySnapshotId,
                registrationOrderId,
                registrationOrderLineId,
                Guid.CreateVersion7(),
                user.Id,
                currency,
                commercialDigest,
                entitlementDigest,
                gross,
                funding,
                priority,
                UtcNow);
        EventWaitlistEntry target = Target(
            order.Id,
            line.Id,
            purchasePolicySnapshotId,
            "USD",
            terms,
            entitlement,
            12_345,
            1,
            priority: 0);
        EventWaitlistEntry[] different =
        [
            Target(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "USD",
                terms,
                entitlement,
                12_345,
                1,
                priority: 10),
            Target(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                purchasePolicySnapshotId,
                "EUR",
                terms,
                entitlement,
                12_345,
                1,
                priority: 10),
            Target(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                purchasePolicySnapshotId,
                "USD",
                Digest("other-terms"),
                entitlement,
                12_345,
                1,
                priority: 10),
            Target(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                purchasePolicySnapshotId,
                "USD",
                terms,
                Digest("other-entitlement"),
                12_345,
                1,
                priority: 10),
            Target(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                purchasePolicySnapshotId,
                "USD",
                terms,
                entitlement,
                12_346,
                1,
                priority: 10),
            Target(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                purchasePolicySnapshotId,
                "USD",
                terms,
                entitlement,
                12_345,
                2,
                priority: 10),
        ];
        context.AddRange(
            eventEntity,
            catalog,
            order,
            policy,
            target);
        context.AddRange(different);
        await context.SaveChangesAsync();
        return new WaitlistAccessSeed(
            tenant.Id,
            eventEntity.Id,
            order.Id,
            line.Id);
    }

    private async Task<FairReturnWaitlistResult>
        AllocateSeedAsync(WaitlistSeed seed)
    {
        await using ExploreDbContext context =
            fixture.CreateDbContext();
        return await new FairReturnWaitlistRepository(
                context)
            .AllocateAsync(
                new FairReturnAllocationRequest(
                    seed.TenantId,
                    seed.EventId,
                    seed.PolicyId,
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    UtcNow.AddMinutes(1)),
                CancellationToken.None);
    }

    private static WaitlistEntrySeed EntrySeed(
        int priority,
        DateTime enqueuedAtUtc,
        Guid id) =>
        new(id, priority, enqueuedAtUtc);

    private static FairReturnSupplyUnit Supply(
        Guid tenantId,
        Guid eventId,
        Guid ticketTypeId,
        Guid catalogId,
        Guid policySnapshotId,
        string currencyCode,
        string commercialTermsDigest,
        string entitlementDigest,
        long grossMinorUnits,
        int refundFundingModeId) =>
        FairReturnSupplyUnit.Create(
            Guid.CreateVersion7(),
            tenantId,
            eventId,
            ticketTypeId,
            catalogId,
            policySnapshotId,
            currencyCode,
            commercialTermsDigest,
            entitlementDigest,
            grossMinorUnits,
            refundFundingModeId,
            Guid.CreateVersion7(),
            UtcNow);

    private static string Digest(string value) =>
        Convert.ToBase64String(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value)));

    private static Task<string> ReadSourceAsync(
        string relativePath)
    {
        string path = Path.Combine(
            RepositoryRoot,
            relativePath.Replace(
                '/',
                Path.DirectorySeparatorChar));
        return File.Exists(path)
            ? File.ReadAllTextAsync(path)
            : Task.FromResult(string.Empty);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(
            AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                    directory.FullName,
                    "Explore.slnx")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root was not found.");
    }

    private sealed class WaitlistRaceGate(
        int participantCount)
    {
        private readonly TaskCompletionSource<bool>
            _allArrived = new(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool>
            _release = new(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);
        private int _arrived;

        public Task AllArrived => _allArrived.Task;

        public async Task ArriveAsync(
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _arrived)
                == participantCount)
            {
                _allArrived.TrySetResult(true);
            }
            await _release.Task.WaitAsync(
                cancellationToken);
        }

        public void Release() =>
            _release.TrySetResult(true);
    }

    private sealed record WaitlistSeed(
        Guid TenantId,
        Guid EventId,
        Guid PolicyId,
        IReadOnlyList<Guid> SupplyIds,
        IReadOnlyList<Guid>
            RegistrationOrderLineIds);

    private sealed record WaitlistAccessSeed(
        Guid TenantId,
        Guid EventId,
        Guid RegistrationOrderId,
        Guid RegistrationOrderLineId);

    private sealed record WaitlistEntrySeed(
        Guid Id,
        int Priority,
        DateTime EnqueuedAtUtc);

    private sealed record OrchestrationSeed(
        Guid TenantId,
        Guid EffectId);
}
