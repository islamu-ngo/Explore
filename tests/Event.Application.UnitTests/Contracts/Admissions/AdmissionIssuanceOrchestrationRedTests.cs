// ABOUTME: Executes the exact planned AdmissionIssuanceService.IssueConfirmedAsync public contract.
// ABOUTME: Covers free replay, paid authority, canonical credential children, and atomic delivery intent persistence.

using ApplicationUnitTests.Contracts.Admissions.Support;
using Explore.Application.Contracts.Admissions;
using System.Reflection;

namespace ApplicationUnitTests.Contracts.Admissions;

public sealed class AdmissionIssuanceOrchestrationRedTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task FreeConfirmedIssuanceCreatesOneTicketPerAssignmentAndReplayKeepsEveryIdentity()
    {
        AdmissionTestScenario scenario = AdmissionTestScenario.Free(UtcNow, Assignments());
        object service = AdmissionIssuancePorts.Service(scenario);
        object request = AdmissionIssuancePorts.Request(scenario);

        object first = await AdmissionContractRuntime.InvokeAsync(
            service, "IssueConfirmedAsync", request, CancellationToken.None);
        int intentsAfterFirst = scenario.PersistedDeliveryIntentCount;
        int dispatchesAfterFirst = scenario.IssuanceDeliveryCalls;
        int commitsAfterFirst = scenario.TransactionCommits;
        object replay = await AdmissionContractRuntime.InvokeAsync(
            service, "IssueConfirmedAsync", request, CancellationToken.None);
        Guid[] firstIds = AdmissionContractRuntime.Ids(first, "IssuedTicketIds");
        Guid[] replayIds = AdmissionContractRuntime.Ids(replay, "ExistingTicketIds");
        object firstCredential = AdmissionContractRuntime.Items(first, "OneTimeCredentials").First();
        string plaintext = AdmissionContractRuntime.Value<string>(firstCredential, "PlaintextCredential");

        await Assert.That(first.ToString()).DoesNotContain(plaintext);
        await Assert.That(firstCredential.ToString()).DoesNotContain(plaintext);
        await Assert.That(firstIds.Length).IsEqualTo(2);
        await Assert.That(firstIds.Distinct().Count()).IsEqualTo(2);
        await Assert.That(replayIds.Order()).IsEquivalentTo(firstIds.Order());
        await Assert.That(scenario.TicketsByAssignment.Keys)
            .IsEquivalentTo(scenario.Assignments.Select(value => value.AssignmentId));
        await Assert.That(scenario.IssuanceWriteCalls).IsEqualTo(1);
        await Assert.That(scenario.DigestIssueCalls).IsEqualTo(2);
        await Assert.That(intentsAfterFirst).IsEqualTo(2);
        await Assert.That(dispatchesAfterFirst).IsEqualTo(intentsAfterFirst);
        await Assert.That(scenario.PersistedDeliveryIntentCount).IsEqualTo(intentsAfterFirst);
        await Assert.That(scenario.IssuanceDeliveryCalls).IsEqualTo(dispatchesAfterFirst);
        await Assert.That(scenario.TransactionCommits).IsEqualTo(commitsAfterFirst + 1);
        await Assert.That(scenario.IssuanceDispatchCommitCounts.All(value => value == 1)).IsTrue();
        await Assert.That(scenario.PendingDeliveryIntentIds).IsEmpty();
        await AssertCanonicalCredentialChildrenAsync(scenario.TicketsByAssignment.Values);
    }

    [Test]
    public async Task MalformedLineageAndPreCommitCancellationReturnTypedFailuresWithoutWriting()
    {
        AdmissionTestScenario malformedScenario = AdmissionTestScenario.Free(UtcNow, [Assignments()[0]]);
        object malformedRequest = AdmissionContractRuntime.ApplicationObject(
            "AdmissionIssuanceRequest",
            ("TenantId", Guid.Empty),
            ("RegistrationOrderId", malformedScenario.OrderId),
            ("FinalizationEffectId", malformedScenario.FinalizationEffectId),
            ("Authority", malformedScenario.Authority));
        object malformed = await AdmissionContractRuntime.InvokeAsync(
            AdmissionIssuancePorts.Service(malformedScenario), "IssueConfirmedAsync", malformedRequest, CancellationToken.None);

        AdmissionTestScenario cancelledScenario = AdmissionTestScenario.Free(UtcNow, [Assignments()[0]]);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        object cancelledResult = await AdmissionContractRuntime.InvokeAsync(
            AdmissionIssuancePorts.Service(cancelledScenario),
            "IssueConfirmedAsync",
            AdmissionIssuancePorts.Request(cancelledScenario),
            cancelled.Token);

        await Assert.That(AdmissionContractRuntime.Value<AdmissionIssuanceOutcome>(malformed, "Outcome"))
            .IsEqualTo(AdmissionIssuanceOutcome.InvalidRequest);
        await Assert.That(AdmissionContractRuntime.Value<AdmissionIssuanceOutcome>(cancelledResult, "Outcome"))
            .IsEqualTo(AdmissionIssuanceOutcome.CancelledBeforeCommit);
        await Assert.That(malformedScenario.IssuanceWriteCalls).IsEqualTo(0);
        await Assert.That(cancelledScenario.IssuanceWriteCalls).IsEqualTo(0);
    }

    [Test]
    public async Task PaidSignalsAreDeniedUntilTheLaterPaidAdmissionIntegrationPhase()
    {
        AdmissionAssignmentSeed assignment = Assignments()[0];
        AdmissionTestScenario paymentOnly = AdmissionTestScenario.Paid(UtcNow, [assignment], reconciled: false);
        object denied = await AdmissionContractRuntime.InvokeAsync(
            AdmissionIssuancePorts.Service(paymentOnly),
            "IssueConfirmedAsync",
            AdmissionIssuancePorts.Request(paymentOnly),
            CancellationToken.None);

        await Assert.That(AdmissionContractRuntime.Value<AdmissionIssuanceOutcome>(denied, "Outcome"))
            .IsEqualTo(AdmissionIssuanceOutcome.NotConfirmed);
        await Assert.That(paymentOnly.TicketsByAssignment).IsEmpty();
        await Assert.That(paymentOnly.IssuanceWriteCalls).IsEqualTo(0);

        AdmissionTestScenario reconciled = AdmissionTestScenario.Paid(UtcNow, [assignment], reconciled: true);
        object accepted = await AdmissionContractRuntime.InvokeAsync(
            AdmissionIssuancePorts.Service(reconciled),
            "IssueConfirmedAsync",
            AdmissionIssuancePorts.Request(reconciled),
            CancellationToken.None);

        await Assert.That(AdmissionContractRuntime.Value<AdmissionIssuanceOutcome>(accepted, "Outcome"))
            .IsEqualTo(AdmissionIssuanceOutcome.NotConfirmed);
        await Assert.That(AdmissionContractRuntime.Ids(accepted, "IssuedTicketIds")).IsEmpty();
        await Assert.That(reconciled.TicketsByAssignment).IsEmpty();
        await Assert.That(reconciled.IssuanceWriteCalls).IsEqualTo(0);
    }

    [Test]
    public async Task TicketAndDeliveryIntentPersistenceIsAtomicAndDispatchIsPostCommit()
    {
        AdmissionTestScenario scenario = AdmissionTestScenario.Free(UtcNow, Assignments());
        _ = await AdmissionContractRuntime.InvokeAsync(
            AdmissionIssuancePorts.Service(scenario),
            "IssueConfirmedAsync",
            AdmissionIssuancePorts.Request(scenario),
            CancellationToken.None);

        await Assert.That(scenario.IssuanceWriteCalls).IsEqualTo(1);
        await Assert.That(scenario.AtomicTicketAndIntentWriteObserved).IsTrue();
        await Assert.That(scenario.TicketsByAssignment.Count).IsEqualTo(2);
        await Assert.That(scenario.PersistedDeliveryIntentCount).IsEqualTo(2);
        await Assert.That(scenario.IssuanceDeliveryCalls).IsEqualTo(scenario.PersistedDeliveryIntentCount);
        await Assert.That(scenario.TransactionCommits).IsEqualTo(1);
        await Assert.That(scenario.IssuanceDispatchCommitCounts).IsEquivalentTo([1, 1]);
        await Assert.That(scenario.DeliveryCalledInsideTransaction).IsFalse();
        await Assert.That(scenario.PendingDeliveryIntentIds).IsEmpty();
    }

    [Test]
    public async Task LostCommitAcknowledgementReconcilesCommittedRowsAndMaterialBeforeReporting()
    {
        AdmissionTestScenario scenario = AdmissionTestScenario.Free(UtcNow, [Assignments()[0]]);
        scenario.LoseNextCommitAcknowledgement = true;

        object result = await AdmissionContractRuntime.InvokeAsync(
            AdmissionIssuancePorts.Service(scenario),
            "IssueConfirmedAsync",
            AdmissionIssuancePorts.Request(scenario),
            CancellationToken.None);

        await Assert.That(AdmissionContractRuntime.Value<AdmissionIssuanceOutcome>(result, "Outcome"))
            .IsEqualTo(AdmissionIssuanceOutcome.AlreadyIssued);
        await Assert.That(AdmissionContractRuntime.Value<AdmissionDeliveryOutcome>(result, "DeliveryOutcome"))
            .IsEqualTo(AdmissionDeliveryOutcome.Delivered);
        await Assert.That(AdmissionContractRuntime.Ids(result, "ExistingTicketIds").Length).IsEqualTo(1);
        await Assert.That(AdmissionContractRuntime.Items(result, "OneTimeCredentials").Length).IsEqualTo(1);
        await Assert.That(scenario.TransactionCommits).IsEqualTo(1);
        await Assert.That(scenario.DigestIssueCalls).IsEqualTo(1);
        await Assert.That(scenario.PersistedDeliveryIntentCount).IsEqualTo(1);
        await Assert.That(scenario.IssuanceDeliveryCalls).IsEqualTo(1);
    }

    [Test]
    public async Task DispatcherFailureLeavesCommittedIntentAvailableForRetry()
    {
        AdmissionTestScenario scenario = AdmissionTestScenario.Free(UtcNow, [Assignments()[0]]);
        scenario.FailNextIssuanceDelivery = true;

        object committed = await AdmissionContractRuntime.InvokeAsync(
            AdmissionIssuancePorts.Service(scenario),
            "IssueConfirmedAsync",
            AdmissionIssuancePorts.Request(scenario),
            CancellationToken.None);
        object replay = await AdmissionContractRuntime.InvokeAsync(
            AdmissionIssuancePorts.Service(scenario),
            "IssueConfirmedAsync",
            AdmissionIssuancePorts.Request(scenario),
            CancellationToken.None);

        await Assert.That(AdmissionContractRuntime.Value<AdmissionIssuanceOutcome>(committed, "Outcome"))
            .IsEqualTo(AdmissionIssuanceOutcome.Issued);
        await Assert.That(AdmissionContractRuntime.Value<AdmissionDeliveryOutcome>(committed, "DeliveryOutcome"))
            .IsEqualTo(AdmissionDeliveryOutcome.RecoverablePending);
        await Assert.That(AdmissionContractRuntime.Value<AdmissionDeliveryFailure>(committed, "DeliveryFailure"))
            .IsEqualTo(AdmissionDeliveryFailure.RouteUnavailable);
        await Assert.That(AdmissionContractRuntime.Value<AdmissionDeliveryOutcome>(replay, "DeliveryOutcome"))
            .IsEqualTo(AdmissionDeliveryOutcome.Delivered);
        await Assert.That(AdmissionContractRuntime.Items(replay, "OneTimeCredentials").Length).IsEqualTo(1);
        await Assert.That(scenario.TransactionCommits).IsEqualTo(2);
        await Assert.That(scenario.TicketsByAssignment.Count).IsEqualTo(1);
        await Assert.That(scenario.PersistedDeliveryIntentCount).IsEqualTo(1);
        await Assert.That(scenario.IssuanceDeliveryCalls).IsEqualTo(2);
        await Assert.That(scenario.IssuanceDispatchCommitCounts).IsEquivalentTo([1, 2]);
        await Assert.That(scenario.DeliveryCalledInsideTransaction).IsFalse();
        await Assert.That(scenario.PendingDeliveryIntentIds).IsEmpty();
    }

    internal static AdmissionAssignmentSeed[] Assignments()
    {
        Guid lineId = Guid.CreateVersion7();
        return
        [
            new(Guid.CreateVersion7(), lineId, Guid.CreateVersion7(), 500, true),
            new(Guid.CreateVersion7(), lineId, Guid.CreateVersion7(), 500, true)
        ];
    }

    private static async Task AssertCanonicalCredentialChildrenAsync(IEnumerable<object> tickets)
    {
        Type ticketType = AdmissionContractRuntime.DomainType("AdmissionTicket");
        Type credentialType = AdmissionContractRuntime.DomainType("AdmissionTicketCredential");
        foreach (object ticket in tickets)
        {
            await Assert.That(ticket.GetType()).IsEqualTo(ticketType);
            PropertyInfo credentialsProperty = ticketType.GetProperty("Credentials", BindingFlags.Instance | BindingFlags.Public)
                ?? throw AdmissionContractRuntime.Missing("AdmissionTicket.Credentials bounded child history");
            object[] credentials = ((System.Collections.IEnumerable)credentialsProperty.GetValue(ticket)!).Cast<object>().ToArray();
            await Assert.That(credentials.Length).IsEqualTo(1);
            await Assert.That(credentials.Single().GetType()).IsEqualTo(credentialType);
        }
    }
}
