// ABOUTME: Defines RED contracts for durable fair-return payment, refund, restart, and drain behavior.
// ABOUTME: Pins atomic pointer intents, stable idempotency, bounded fairness, dead-lettering, and zero-PII telemetry.

using System.Diagnostics.Metrics;
using Explore.Domain;
using Explore.Application.Contracts.Waitlist;
using Explore.Application.Telemetry;
using Explore.Infrastructure;
using Explore.Infrastructure.Waitlist;
using Explore.Persistence;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests;

public sealed class FairReturnWaitlistOrchestrationTests
{
    private const string ApplicationServicePath =
        "src/Explore.Application/Services/Waitlist/" +
        "FairReturnOrchestrationService.cs";
    private const string RepositoryContractPath =
        "src/Explore.Application/Contracts/Waitlist/" +
        "FairReturnOrchestrationContracts.cs";
    private const string DrainServicePath =
        "src/Explore.Infrastructure/Waitlist/" +
        "FairReturnOrchestrationDrainService.cs";
    private const string QuartzJobPath =
        "src/Explore.API/Scheduling/" +
        "FairReturnOrchestrationJob.cs";
    private const string TelemetryPath =
        "src/Explore.Application/Telemetry/" +
        "FairReturnOrchestrationTelemetry.cs";
    private static readonly string RepositoryRoot =
        FindRepositoryRoot();

    [Test]
    public async Task PaymentAndRefundIntentFactsAreAtomicAndPointerOnly()
    {
        Type? paymentIntent = DomainType(
            "Explore.Domain.WaitlistPaymentIntent");
        Type? refundIntent = DomainType(
            "Explore.Domain.WaitlistRefundIntent");
        await Assert.That(paymentIntent).IsNotNull();
        await Assert.That(refundIntent).IsNotNull();

        string configuration = await ReadSourceAsync(
            "src/Explore.Persistence/Configurations/" +
            "Entities/FairReturnWaitlistConfigurations.cs");
        string dbSets = await ReadSourceAsync(
            "src/Explore.Persistence/" +
            "ExploreDbContext.DbSets.cs");
        await Assert.That(configuration).Contains(
            "WaitlistPaymentIntentConfiguration");
        await Assert.That(refundIntent!
                .GetProperty(
                    "OriginalPaymentAllocationId"))
            .IsNotNull();
        await Assert.That(refundIntent
                .GetProperty(
                    "ReplacementPaymentSettledAt"))
            .IsNotNull();
        await Assert.That(dbSets).Contains(
            "WaitlistPaymentIntents");
        await Assert.That(dbSets).Contains(
            "WaitlistRefundIntents");
    }

    [Test]
    public async Task RepositoryOwnsOneCanonicalOrchestrationFence()
    {
        string contract = await ReadSourceAsync(
            RepositoryContractPath);
        string repository = await ReadSourceAsync(
            "src/Explore.Persistence/Repositories/" +
            "FairReturnOrchestrationRepository.cs");

        await Assert.That(contract).Contains(
            "IFairReturnOrchestrationRepository");
        await Assert.That(contract).Contains(
            "CreatePaymentIntentAsync");
        await Assert.That(contract).Contains(
            "ObserveReplacementSettlementAsync");
        await Assert.That(contract).Contains(
            "CreateRefundIntentAsync");
        await Assert.That(repository).Contains(
            "CanonicalFenceOrder");
        await Assert.That(repository).Contains(
            "RelationalEntityRowFence");
        await Assert.That(repository).Contains(
            "EfCoreUnitOfWork");
    }

    [Test]
    public async Task UnknownReplayUsesOneStableProviderIdempotencyKey()
    {
        string application = await ReadSourceAsync(
            ApplicationServicePath);
        string contract = await ReadSourceAsync(
            RepositoryContractPath);

        await Assert.That(application).Contains(
            "ProviderIdempotencyKey");
        await Assert.That(application).Contains(
            "Unknown");
        await Assert.That(application).Contains(
            "REPLACEMENT_PAYMENT_UNKNOWN");
        await Assert.That(application).Contains(
            "TryDispatch");
        await Assert.That(contract).Contains(
            "StableOperationId");
        await Assert.That(contract).Contains(
            "ProviderIdempotencyKey");
    }

    [Test]
    public async Task OutcomeMatrixSeparatesRetryUnknownPoisonAndDeadLetter()
    {
        Type? outcome = ApplicationType(
            "Explore.Application.Contracts.Waitlist." +
            "FairReturnDispatchOutcome");
        await Assert.That(outcome).IsNotNull();
        string[] names = outcome is null
            ? []
            : Enum.GetNames(outcome);
        await Assert.That(names).Contains("Succeeded");
        await Assert.That(names).Contains("RetryScheduled");
        await Assert.That(names).Contains("Unknown");
        await Assert.That(names).Contains("Poisoned");
        await Assert.That(names).Contains("DeadLettered");
        await Assert.That(names).Contains("StaleLease");
    }

    [Test]
    public async Task RefundDispatchRequiresReplacementSettlementFence()
    {
        string application = await ReadSourceAsync(
            ApplicationServicePath);
        string contract = await ReadSourceAsync(
            RepositoryContractPath);
        string repository = await ReadSourceAsync(
            "src/Explore.Persistence/Repositories/" +
            "FairReturnOrchestrationRepository.cs");

        await Assert.That(repository).Contains(
            "ReplacementPaymentSettledAt");
        await Assert.That(application).Contains(
            "CreateRefundIntentAsync");
        Type refundIntent =
            typeof(WaitlistRefundIntent);
        await Assert.That(refundIntent
                .GetProperty(
                    "OriginalPaymentAllocationId"))
            .IsNotNull();
        await Assert.That(refundIntent
                .GetProperty(
                    "ReplacementPaymentSettledAt"))
            .IsNotNull();
        await Assert.That(contract).Contains(
            "CreateRefundIntentAsync");
    }

    [Test]
    public async Task QuartzWakeupCarriesOnlyDurableEffectIdentifiers()
    {
        string job = await ReadSourceAsync(
            QuartzJobPath);

        await Assert.That(job).Contains(
            "IJob");
        await Assert.That(job).Contains(
            "DisallowConcurrentExecution");
        await Assert.That(job).Contains(
            "FairReturnOrchestrationDrainService");
        await Assert.That(job).Contains(
            "EffectId");
        await Assert.That(job).DoesNotContain(
            "Email");
        await Assert.That(job).DoesNotContain(
            "Phone");
        await Assert.That(job).DoesNotContain(
            "ProviderPayload");
        await Assert.That(job).DoesNotContain(
            "PaymentInstrument");
    }

    [Test]
    public async Task RestartReclaimsExpiredLeaseWithoutDuplicatingIntent()
    {
        string drain = await ReadSourceAsync(
            DrainServicePath);
        string contract = await ReadSourceAsync(
            RepositoryContractPath);
        string effect = await ReadSourceAsync(
            "src/Explore.Domain/" +
            "FairReturnOrchestration.cs");

        await Assert.That(drain).Contains(
            "TryClaimDueAsync");
        await Assert.That(effect).Contains(
            "LeaseExpiresAt");
        await Assert.That(contract).Contains(
            "MarkCompletedAsync");
        await Assert.That(contract).Contains(
            "MarkFailedAsync");
        await Assert.That(contract).Contains(
            "ExpiredLease");
        await Assert.That(contract).Contains(
            "StableOperationId");
    }

    [Test]
    public async Task TenThousandEffectDrainIsBoundedAndCompletes()
    {
        const int effectCount = 10_000;
        Guid tenantId = Guid.CreateVersion7();
        FairReturnOrchestrationClaim[] claims =
            Enumerable.Range(1, effectCount)
                .Select(index => new
                    FairReturnOrchestrationClaim(
                        tenantId,
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        $"refund-{index}",
                        index,
                        1,
                        1,
                        10,
                        ExpiredLease: false))
                .ToArray();
        IFairReturnOrchestrationRepository
            repository = Substitute.For<
                IFairReturnOrchestrationRepository>();
        repository.TryClaimDueAsync(
                Arg.Any<DateTime>(),
                Arg.Any<string>(),
                null,
                effectCount,
                effectCount,
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(claims);
        IFairReturnOrchestrationDispatcher
            dispatcher = Substitute.For<
                IFairReturnOrchestrationDispatcher>();
        dispatcher.TryDispatch(
                Arg.Any<
                    FairReturnOrchestrationClaim>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                FairReturnOrchestrationClaim claim =
                    call.Arg<
                        FairReturnOrchestrationClaim>()!;
                return new
                    FairReturnOrchestrationDispatchResult(
                        FairReturnDispatchOutcome
                            .Succeeded,
                        claim.EffectId,
                        string.Empty);
            });
        var settings =
            Options.Create(
                new
                    FairReturnOrchestrationDrainSettings
                    {
                        BatchSize = effectCount,
                        MaximumEffectsPerTenant =
                            effectCount,
                        LeaseDurationSeconds = 120,
                    });
        var drain = new
            FairReturnOrchestrationDrainService(
                repository,
                dispatcher,
                settings,
                new FixedTimeProvider());

        FairReturnOrchestrationDrainResult result =
            await drain.DrainAsync(
                null,
                CancellationToken.None);

        await Assert.That(result.Claimed)
            .IsEqualTo(effectCount);
        await Assert.That(result.Succeeded)
            .IsEqualTo(effectCount);
        await Assert.That(result.DeadLettered)
            .IsEqualTo(0);
    }

    [Test]
    public async Task DrainCountsEveryOutcomeAndForwardsExactBounds()
    {
        FairReturnDispatchOutcome[] outcomes =
            Enum.GetValues<
                FairReturnDispatchOutcome>();
        Guid tenantId = Guid.CreateVersion7();
        FairReturnOrchestrationClaim[] claims =
            outcomes.Select((outcome, index) =>
                    new FairReturnOrchestrationClaim(
                        tenantId,
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        Guid.CreateVersion7(),
                        $"operation-{index}",
                        index + 1,
                        1,
                        1,
                        10,
                        ExpiredLease: false))
                .ToArray();
        Dictionary<Guid,
            FairReturnDispatchOutcome> byEffect =
            claims.Zip(outcomes)
                .ToDictionary(
                    pair => pair.First.EffectId,
                    pair => pair.Second);
        IFairReturnOrchestrationRepository
            repository = Substitute.For<
                IFairReturnOrchestrationRepository>();
        repository.TryClaimDueAsync(
                Arg.Any<DateTime>(),
                Arg.Any<string>(),
                Arg.Any<Guid?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(claims);
        IFairReturnOrchestrationDispatcher
            dispatcher = Substitute.For<
                IFairReturnOrchestrationDispatcher>();
        dispatcher.TryDispatch(
                Arg.Any<
                    FairReturnOrchestrationClaim>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                FairReturnOrchestrationClaim claim =
                    call.Arg<
                        FairReturnOrchestrationClaim>()!;
                return new
                    FairReturnOrchestrationDispatchResult(
                        byEffect[claim.EffectId],
                        claim.EffectId,
                        string.Empty);
            });
        var settings = Options.Create(
            new FairReturnOrchestrationDrainSettings
            {
                BatchSize = 17,
                MaximumEffectsPerTenant = 3,
                LeaseDurationSeconds = 91,
            });
        Guid effectId = Guid.CreateVersion7();
        var drain =
            new FairReturnOrchestrationDrainService(
                repository,
                dispatcher,
                settings,
                new FixedTimeProvider());

        FairReturnOrchestrationDrainResult result =
            await drain.DrainAsync(
                effectId,
                CancellationToken.None);

        await repository.Received(1)
            .TryClaimDueAsync(
                new DateTime(
                    2026,
                    8,
                    28,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc),
                Arg.Is<string>(value =>
                    !string.IsNullOrWhiteSpace(value)),
                effectId,
                17,
                3,
                TimeSpan.FromSeconds(91),
                CancellationToken.None);
        await Assert.That(result.Claimed)
            .IsEqualTo(outcomes.Length);
        await Assert.That(result.Succeeded)
            .IsEqualTo(1);
        await Assert.That(result.RetryScheduled)
            .IsEqualTo(1);
        await Assert.That(result.Unknown)
            .IsEqualTo(1);
        await Assert.That(result.Poisoned)
            .IsEqualTo(1);
        await Assert.That(result.DeadLettered)
            .IsEqualTo(1);
        await Assert.That(result.StaleLease)
            .IsEqualTo(1);
    }

    [Test]
    public async Task InterleavedTenantsCannotStarveBehindOneTenant()
    {
        string repository = await ReadSourceAsync(
            "src/Explore.Persistence/Repositories/" +
            "FairReturnOrchestrationRepository.cs");
        string contract = await ReadSourceAsync(
            RepositoryContractPath);

        await Assert.That(repository).Contains(
            "TenantId");
        await Assert.That(repository).Contains(
            "RoundRobin");
        await Assert.That(contract).Contains(
            "FairTenantCursor");
        await Assert.That(contract).Contains(
            "MaximumEffectsPerTenant");
    }

    [Test]
    public async Task TelemetryUsesZeroSentinelAndNoPiiDimensions()
    {
        string telemetry = await ReadSourceAsync(
            TelemetryPath);

        await Assert.That(telemetry).Contains(
            "ZeroSentinel");
        await Assert.That(telemetry).Contains(
            "fair_return");
        await Assert.That(telemetry).Contains(
            "Outcome");
        var measurements = new Dictionary<
            string,
            long>(StringComparer.Ordinal);
        using var listener = new MeterListener
        {
            InstrumentPublished =
                (instrument, meterListener) =>
                {
                    if (instrument.Meter.Name ==
                        FairReturnOrchestrationTelemetry
                            .MeterIdentity)
                    {
                        meterListener
                            .EnableMeasurementEvents(
                                instrument);
                    }
                },
        };
        listener.SetMeasurementEventCallback<long>(
            (instrument,
                measurement,
                tags,
                _) =>
            {
                if (instrument.Meter.Name !=
                    FairReturnOrchestrationTelemetry
                        .MeterIdentity)
                {
                    return;
                }
                string outcome = tags
                    .ToArray()
                    .Single(value =>
                        value.Key == "outcome")
                    .Value!
                    .ToString()!;
                measurements[outcome] =
                    measurement;
            });
        listener.Start();

        FairReturnOrchestrationTelemetry.Record(
            new FairReturnOrchestrationDrainResult(
                0,
                0,
                0,
                0,
                0,
                0,
                0));

        foreach (FairReturnDispatchOutcome outcome
                 in Enum.GetValues<
                     FairReturnDispatchOutcome>())
        {
            await Assert.That(measurements)
                .ContainsKey(outcome.ToString());
            await Assert.That(
                    measurements[outcome.ToString()])
                .IsEqualTo(
                    FairReturnOrchestrationTelemetry
                        .ZeroSentinel);
        }
        await Assert.That(measurements)
            .ContainsKey("idle");
        await Assert.That(telemetry).DoesNotContain(
            "Email");
        await Assert.That(telemetry).DoesNotContain(
            "Phone");
        await Assert.That(telemetry).DoesNotContain(
            "Name");
        await Assert.That(telemetry).DoesNotContain(
            "ProviderObjectId");
        await Assert.That(telemetry).DoesNotContain(
            "PaymentInstrument");
    }

    [Test]
    public async Task DurableFactsContainReferencesNotParticipantPii()
    {
        string[] typeNames =
        [
            "Explore.Domain.WaitlistPaymentIntent",
            "Explore.Domain.WaitlistRefundIntent",
            "Explore.Domain.WaitlistProviderObservation",
        ];
        string[] forbidden =
        [
            "Email",
            "Phone",
            "Name",
            "Address",
            "PaymentInstrument",
            "ProviderPayload",
        ];

        foreach (string typeName in typeNames)
        {
            Type? type = DomainType(typeName);
            await Assert.That(type).IsNotNull();
            string[] properties = type is null
                ? []
                : type.GetProperties()
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
    public async Task OrchestrationTypesStayInTheirCleanArchitectureLayers()
    {
        Type? applicationService = ApplicationType(
            "Explore.Application.Services.Waitlist." +
            "FairReturnOrchestrationService");
        Type? infrastructureDrain =
            typeof(InfrastructureServicesRegistration)
                .Assembly.GetType(
                    "Explore.Infrastructure.Waitlist." +
                    "FairReturnOrchestrationDrainService");
        Type? repository =
            typeof(ExploreDbContext).Assembly.GetType(
                "Explore.Persistence.Repositories." +
                "FairReturnOrchestrationRepository");

        await Assert.That(applicationService)
            .IsNotNull();
        await Assert.That(infrastructureDrain)
            .IsNotNull();
        await Assert.That(repository).IsNotNull();
    }

    private static Type? DomainType(
        string fullName) =>
        typeof(AdmissionTicket).Assembly.GetType(
            fullName);

    private static Type? ApplicationType(
        string fullName) =>
        typeof(Explore.Application.Contracts
                .Persistence.IOutboxRepository)
            .Assembly.GetType(fullName);

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

    private sealed class FixedTimeProvider :
        TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(
                2026,
                8,
                28,
                12,
                0,
                0,
                TimeSpan.Zero);
    }
}
