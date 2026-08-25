// ABOUTME: Executes AdmissionRevocationService.ReconcileAsync against the complete per-line refund matrix.
// ABOUTME: Requires every result to partition exact issued ticket identities into revoked and preserved sets.

using ApplicationUnitTests.Contracts.Admissions.Support;

namespace ApplicationUnitTests.Contracts.Admissions;

public sealed class AdmissionRevocationOrchestrationRedTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    [Arguments("full-relevant")]
    [Arguments("one-relevant-partial")]
    [Arguments("add-on-only")]
    [Arguments("mixed-full-ticket-and-addon")]
    [Arguments("multiple-ticket-lines-partial")]
    [Arguments("zero-relevant")]
    [Arguments("negative")]
    [Arguments("over-allocation")]
    [Arguments("cancellation")]
    public async Task RefundAndCancellationReturnExactRevokedAndPreservedTicketIdentities(string matrixCase)
    {
        AdmissionTestScenario scenario = AdmissionTestScenario.Free(UtcNow, AdmissionRevocationRow.Assignments());
        object issued = await AdmissionContractRuntime.InvokeAsync(
            AdmissionIssuancePorts.Service(scenario),
            "IssueConfirmedAsync",
            AdmissionIssuancePorts.Request(scenario),
            CancellationToken.None);
        await Assert.That(AdmissionContractRuntime.Ids(issued, "IssuedTicketIds").Length).IsEqualTo(3);

        AdmissionRevocationRow row = AdmissionRevocationRow.For(matrixCase, scenario);
        object result = await AdmissionContractRuntime.InvokeAsync(
            AdmissionRevocationPorts.Service(scenario),
            "ReconcileAsync",
            AdmissionRevocationPorts.Request(scenario, row),
            CancellationToken.None);
        Guid[] revoked = AdmissionContractRuntime.Ids(result, "RevokedTicketIds");
        Guid[] preserved = AdmissionContractRuntime.Ids(result, "PreservedTicketIds");

        await Assert.That(revoked.Order()).IsEquivalentTo(row.ExpectedRevoked.Order());
        await Assert.That(preserved.Order()).IsEquivalentTo(row.ExpectedPreserved.Order());
        await Assert.That(revoked.Intersect(preserved)).IsEmpty();
        await Assert.That(revoked.Concat(preserved).Order()).IsEquivalentTo(
            scenario.TicketsByAssignment.Values.Select(AdmissionContractRuntime.EntityId).Order());

        if (row.Invalid)
        {
            await Assert.That(AdmissionContractRuntime.Outcome(result)).IsEqualTo("InvalidAllocation");
            await Assert.That(scenario.RevocationWriteCalls).IsEqualTo(0);
        }
        else
        {
            await Assert.That(AdmissionContractRuntime.Outcome(result)).IsEqualTo("Applied");
            await Assert.That(scenario.RevocationWriteCalls).IsEqualTo(1);
        }
    }
}
