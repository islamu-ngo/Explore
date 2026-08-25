// ABOUTME: Holds deterministic trusted facts and observable persisted effects for admission orchestration tests.
// ABOUTME: Recovery storage retains digests and bounded metadata only; plaintext exists only at the delivery fake.

using System.Security.Cryptography;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;

namespace ApplicationUnitTests.Contracts.Admissions.Support;

internal sealed record AdmissionAssignmentSeed(
    Guid AssignmentId,
    Guid LineId,
    Guid ParticipantId,
    long LineUnitMinor,
    bool IsAdmissionLine);

internal sealed record StoredRecoveryCapability(
    Guid TenantId,
    Guid RecoveryRequestId,
    Guid AdmissionTicketId,
    string Digest,
    string Purpose,
    DateTimeOffset ExpiresAtUtc,
    bool Consumed,
    bool Rotated);

internal sealed class AdmissionTestScenario
{
    private readonly Queue<string> deliveredCapabilities = new();

    private AdmissionTestScenario(ManualAdmissionTimeProvider clock, IReadOnlyList<AdmissionAssignmentSeed> assignments)
    {
        Clock = clock;
        Assignments = assignments;
        (Order, Catalog, AssignmentFacts) = BuildAuthority(clock.GetUtcNow().UtcDateTime, assignments);
        TenantId = Order.TenantId;
        EventId = Order.EventId;
        OrderId = Order.Id;
        FinalizationEffectId = Guid.CreateVersion7();
        RecoveryRequestId = Guid.CreateVersion7();
        UnitOfWork = new AdmissionTrackingUnitOfWork(this);
    }

    internal static AdmissionTestScenario Free(DateTime now, IReadOnlyList<AdmissionAssignmentSeed> assignments) =>
        new(new ManualAdmissionTimeProvider(now), assignments)
        {
            Authority = "ConfirmedFreeOrder",
            IdentityPresent = true
        };

    internal static AdmissionTestScenario Paid(
        DateTime now,
        IReadOnlyList<AdmissionAssignmentSeed> assignments,
        bool reconciled) => new(new ManualAdmissionTimeProvider(now), assignments)
        {
            Authority = reconciled ? "ReconciledPaidFinalization" : "PaymentSucceeded",
            PaymentReconciled = reconciled,
            IdentityPresent = true
        };

    internal static AdmissionTestScenario Recovery(DateTime now, bool identityPresent)
    {
        AdmissionAssignmentSeed[] assignments = identityPresent
            ? [new(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 0, true)]
            : [];
        return new(new ManualAdmissionTimeProvider(now), assignments)
        {
            Authority = "ConfirmedFreeOrder",
            IdentityPresent = identityPresent
        };
    }

    internal ManualAdmissionTimeProvider Clock { get; }
    internal AdmissionTrackingUnitOfWork UnitOfWork { get; }
    internal IReadOnlyList<AdmissionAssignmentSeed> Assignments { get; }
    internal RegistrationOrder Order { get; }
    internal EventTicketCatalogVersion Catalog { get; }
    internal IReadOnlyList<(RegistrationOrderLine Line, RegistrationTicketAssignment Assignment, RegistrationParticipant Participant, EventTicketType TicketType)> AssignmentFacts { get; }
    internal Dictionary<Guid, object> TicketsByAssignment { get; } = [];
    internal Dictionary<Guid, object> DeliveryIntentsById { get; } = [];
    internal Dictionary<string, StoredRecoveryCapability> RecoveryByDigest { get; } = new(StringComparer.Ordinal);
    internal HashSet<Guid> PendingDeliveryIntentIds { get; } = [];
    internal List<int> IssuanceDispatchCommitCounts { get; } = [];
    internal Guid TenantId { get; }
    internal Guid EventId { get; }
    internal Guid OrderId { get; }
    internal Guid FinalizationEffectId { get; }
    internal Guid RecoveryRequestId { get; }
    internal string NormalizedIdentity { get; } = "holder@example.test";
    internal byte[] AdmissionCredentialDigest { get; } = RandomNumberGenerator.GetBytes(32);
    internal string Authority { get; private init; } = null!;
    internal bool PaymentReconciled { get; private init; }
    internal bool IdentityPresent { get; private init; }
    internal int IssuanceWriteCalls { get; set; }
    internal int RevocationWriteCalls { get; set; }
    internal int PersistedDeliveryIntentCount { get; set; }
    internal bool AtomicTicketAndIntentWriteObserved { get; set; }
    internal bool DeliveryCalledInsideTransaction { get; set; }
    internal int IssuanceDeliveryCalls { get; private set; }
    internal int RecoveryDeliveryCalls { get; private set; }
    internal int DigestIssueCalls { get; set; }
    internal int TransactionCommits { get; set; }
    internal int RecoveryStoreCalls { get; set; }
    internal int RecoveryCurrentReadCalls { get; set; }
    internal int RecoveryRotationCalls { get; set; }
    internal bool FailNextIssuanceDelivery { get; set; }
    internal bool LoseNextCommitAcknowledgement { get; set; }
    internal int StoredRecoveryPlaintextCount => RecoveryByDigest.Values.Sum(value => value.GetType()
        .GetProperties()
        .Count(property => property.Name.Contains("Capability", StringComparison.OrdinalIgnoreCase) &&
                           property.GetValue(value) is string));
    internal int ConsumedRecoveryCount => RecoveryByDigest.Values.Count(value => value.Consumed);
    internal int ActiveRecoveryCount => RecoveryByDigest.Values.Count(value => !value.Consumed && !value.Rotated);
    internal Guid CurrentAdmissionTicketId => TicketsByAssignment.Count == 1
        ? AdmissionContractRuntime.EntityId(TicketsByAssignment.Values.Single())
        : throw AdmissionContractRuntime.Missing("one issued admission ticket for recovery");

    internal void RecordIssuanceDispatch(Guid deliveryIntentId)
    {
        if (!PendingDeliveryIntentIds.Contains(deliveryIntentId))
            throw AdmissionContractRuntime.Missing("persisted delivery intent before dispatch");
        IssuanceDeliveryCalls++;
        IssuanceDispatchCommitCounts.Add(TransactionCommits);
        DeliveryCalledInsideTransaction |= UnitOfWork.InTransaction;
        if (FailNextIssuanceDelivery)
        {
            FailNextIssuanceDelivery = false;
            throw new AdmissionDeliveryUnavailableException();
        }
        PendingDeliveryIntentIds.Remove(deliveryIntentId);
    }

    internal void DeliverCapability(string capability)
    {
        RecoveryDeliveryCalls++;
        DeliveryCalledInsideTransaction |= UnitOfWork.InTransaction;
        deliveredCapabilities.Enqueue(capability);
    }

    internal string TakeDeliveredCapability() => deliveredCapabilities.TryDequeue(out string? value)
        ? value
        : throw AdmissionContractRuntime.Missing("one-time recovery delivery");

    internal Guid[] TicketIdsForLine(Guid lineId) => TicketsByAssignment
        .Where(pair => Assignments.Single(seed => seed.AssignmentId == pair.Key).LineId == lineId)
        .Select(pair => AdmissionContractRuntime.EntityId(pair.Value))
        .ToArray();

    private static (RegistrationOrder Order, EventTicketCatalogVersion Catalog,
        IReadOnlyList<(RegistrationOrderLine, RegistrationTicketAssignment, RegistrationParticipant, EventTicketType)> Facts)
        BuildAuthority(DateTime now, IReadOnlyList<AdmissionAssignmentSeed> seeds)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(tenantId, eventId, "EUR", 1);
        EventTicketType ticketType = EventTicketType.Create(
            Guid.CreateVersion7(), tenantId, catalog.Id, "Admission", "EUR", TicketPricingModeEnum.Free,
            null, null, null, ParticipantDataCollectionModeEnum.None, null, null, null,
            false, false, null, null, null, null);
        catalog.AddTicketType(ticketType, null);
        catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEvent(ticketType.Id, tenantId, eventId, 1));
        catalog.Publish();
        RegistrationOrder order = RegistrationOrder.Create(
            tenantId, eventId, Guid.CreateVersion7(), null, BookingPartyTypeEnum.Individual, catalog.Id,
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 1, 1, 1, null),
            null, null, "EUR", now, null);
        RegistrationOrderLine line = RegistrationOrderLine.Create(
            catalog, ticketType, order.Id, Math.Max(1, seeds.Count), null, null);
        order.AddLine(line);
        var facts = new List<(RegistrationOrderLine, RegistrationTicketAssignment, RegistrationParticipant, EventTicketType)>();
        for (int index = 0; index < seeds.Count; index++)
        {
            RegistrationParticipant participant = RegistrationParticipant.Create(
                tenantId, order.Id, null, ParticipantTypeEnum.Adult, null);
            RegistrationTicketAssignment assignment = RegistrationTicketAssignment.CreateAssigned(
                seeds[index].AssignmentId, line.Id, index + 1, participant, now);
            order.AddParticipant(participant);
            order.AddAssignment(line, assignment, participant);
            facts.Add((line, assignment, participant, ticketType));
        }
        order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create("EUR", 0, 0, 0, 0));
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, now);
        order.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, now);
        order.TransitionTo(RegistrationOrderStatusEnum.Confirmed, now);
        return (order, catalog, facts);
    }
}

internal sealed class AdmissionDeliveryUnavailableException : Exception;

internal sealed class ManualAdmissionTimeProvider(DateTime utcNow) : TimeProvider
{
    private DateTimeOffset now = new(utcNow);
    public override DateTimeOffset GetUtcNow() => now;
    internal void Advance(TimeSpan duration) => now = now.Add(duration);
}

internal sealed class AdmissionTrackingUnitOfWork(AdmissionTestScenario scenario) : IUnitOfWork
{
    internal bool InTransaction { get; private set; }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) =>
        ExecuteInTransactionAsync<object?>(async token =>
        {
            await operation(token);
            return null;
        }, ct);

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
        Execute(operation, ct);

    public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
        Execute(operation, ct);

    private async Task<T> Execute<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        InTransaction = true;
        try
        {
            T result = await operation(cancellationToken);
            scenario.TransactionCommits++;
            if (scenario.LoseNextCommitAcknowledgement)
            {
                scenario.LoseNextCommitAcknowledgement = false;
                throw new OperationCanceledException("Commit acknowledgement was lost after durable commit.");
            }
            return result;
        }
        finally
        {
            InTransaction = false;
        }
    }
}
