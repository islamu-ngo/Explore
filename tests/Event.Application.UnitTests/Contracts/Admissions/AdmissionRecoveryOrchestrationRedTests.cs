// ABOUTME: Executes exact AdmissionRecoveryService request, consume, and resend contracts with fixed time.
// ABOUTME: Covers issued-ticket lineage, single use, expiry, scope, atomic rotation, and uniform receipts.

using ApplicationUnitTests.Contracts.Admissions.Support;
using System.Security.Cryptography;
using System.Text;

namespace ApplicationUnitTests.Contracts.Admissions;

public sealed class AdmissionRecoveryOrchestrationRedTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task IssueConsumeAndReplayUsesTicketBoundRandomSingleUseCapability()
    {
        AdmissionTestScenario scenario = await PresentScenarioAsync();
        object service = AdmissionRecoveryPorts.Service(scenario);
        object issued = await AdmissionContractRuntime.InvokeAsync(
            service, "RequestAsync", AdmissionRecoveryPorts.Request(scenario, "TicketRecovery"), CancellationToken.None);
        string capability = scenario.TakeDeliveredCapability();
        bool separateFromAdmissionCredential = !CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(capability)), scenario.AdmissionCredentialDigest);
        object consumeRequest = AdmissionRecoveryPorts.Consume(
            scenario, capability, "TicketRecovery", scenario.TenantId);
        object first = await AdmissionContractRuntime.InvokeAsync(
            service, "ConsumeAsync", consumeRequest, CancellationToken.None);
        object replay = await AdmissionContractRuntime.InvokeAsync(
            service, "ConsumeAsync", consumeRequest, CancellationToken.None);

        await AssertUniformReceiptAsync(issued);
        await Assert.That(AdmissionContractRuntime.Outcome(first)).IsEqualTo("Consumed");
        await Assert.That(AdmissionContractRuntime.Outcome(replay)).IsEqualTo("AlreadyConsumed");
        await Assert.That(separateFromAdmissionCredential).IsTrue();
        await Assert.That(scenario.RecoveryByDigest.Count).IsEqualTo(1);
        await Assert.That(scenario.RecoveryByDigest.Values.Single().AdmissionTicketId)
            .IsEqualTo(scenario.CurrentAdmissionTicketId);
        await Assert.That(scenario.ConsumedRecoveryCount).IsEqualTo(1);
        await Assert.That(scenario.StoredRecoveryPlaintextCount).IsEqualTo(0);
    }

    [Test]
    [Arguments("expired")]
    [Arguments("wrong-purpose")]
    [Arguments("wrong-tenant")]
    public async Task ConsumeRejectsExpiredWrongPurposeAndWrongTenant(string rejection)
    {
        AdmissionTestScenario scenario = await PresentScenarioAsync();
        object service = AdmissionRecoveryPorts.Service(scenario);
        _ = await AdmissionContractRuntime.InvokeAsync(
            service, "RequestAsync", AdmissionRecoveryPorts.Request(scenario, "TicketRecovery"), CancellationToken.None);
        string capability = scenario.TakeDeliveredCapability();
        if (rejection == "expired") scenario.Clock.Advance(TimeSpan.FromHours(2));
        string purpose = rejection == "wrong-purpose" ? "TransferAcceptance" : "TicketRecovery";
        Guid tenantId = rejection == "wrong-tenant" ? Guid.CreateVersion7() : scenario.TenantId;
        object result = await AdmissionContractRuntime.InvokeAsync(
            service, "ConsumeAsync", AdmissionRecoveryPorts.Consume(scenario, capability, purpose, tenantId),
            CancellationToken.None);
        string expected = rejection switch
        {
            "expired" => "Expired",
            "wrong-purpose" => "WrongPurpose",
            "wrong-tenant" => "WrongTenant",
            _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, null)
        };

        await Assert.That(AdmissionContractRuntime.Outcome(result)).IsEqualTo(expected);
        await Assert.That(scenario.ConsumedRecoveryCount).IsEqualTo(0);
    }

    [Test]
    public async Task ResendLoadsCurrentRecordAndAtomicallyRotatesToTicketBoundReplacement()
    {
        AdmissionTestScenario scenario = await PresentScenarioAsync();
        object service = AdmissionRecoveryPorts.Service(scenario);
        _ = await AdmissionContractRuntime.InvokeAsync(
            service, "RequestAsync", AdmissionRecoveryPorts.Request(scenario, "TicketRecovery"), CancellationToken.None);
        string original = scenario.TakeDeliveredCapability();
        int commitsBeforeResend = scenario.TransactionCommits;
        _ = await AdmissionContractRuntime.InvokeAsync(
            service, "ResendAsync", AdmissionRecoveryPorts.Resend(scenario), CancellationToken.None);
        int commitsAfterResend = scenario.TransactionCommits;
        string replacement = scenario.TakeDeliveredCapability();
        bool rotatedValue = !CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(original)),
            SHA256.HashData(Encoding.UTF8.GetBytes(replacement)));
        object oldResult = await AdmissionContractRuntime.InvokeAsync(
            service, "ConsumeAsync",
            AdmissionRecoveryPorts.Consume(scenario, original, "TicketRecovery", scenario.TenantId),
            CancellationToken.None);
        object replacementResult = await AdmissionContractRuntime.InvokeAsync(
            service, "ConsumeAsync",
            AdmissionRecoveryPorts.Consume(scenario, replacement, "TicketRecovery", scenario.TenantId),
            CancellationToken.None);

        await Assert.That(rotatedValue).IsTrue();
        await Assert.That(AdmissionContractRuntime.Outcome(oldResult)).IsEqualTo("Rotated");
        await Assert.That(AdmissionContractRuntime.Outcome(replacementResult)).IsEqualTo("Consumed");
        await Assert.That(scenario.RecoveryCurrentReadCalls).IsEqualTo(1);
        await Assert.That(scenario.RecoveryRotationCalls).IsEqualTo(1);
        await Assert.That(commitsAfterResend).IsEqualTo(commitsBeforeResend + 1);
        await Assert.That(scenario.DeliveryCalledInsideTransaction).IsFalse();
        await Assert.That(scenario.RecoveryByDigest.Count).IsEqualTo(2);
        await Assert.That(scenario.RecoveryByDigest.Values.All(value =>
            value.AdmissionTicketId == scenario.CurrentAdmissionTicketId &&
            value.TenantId == scenario.TenantId && value.RecoveryRequestId == scenario.RecoveryRequestId &&
            value.Purpose == "TicketRecovery")).IsTrue();
        await Assert.That(scenario.ActiveRecoveryCount).IsEqualTo(0);
    }

    [Test]
    public async Task PresentAndAbsentIdentityReturnSameReceiptButAbsentStoresAndDeliversNothing()
    {
        AdmissionTestScenario present = await PresentScenarioAsync();
        AdmissionTestScenario absent = AdmissionTestScenario.Recovery(UtcNow, identityPresent: false);
        object presentResult = await AdmissionContractRuntime.InvokeAsync(
            AdmissionRecoveryPorts.Service(present), "RequestAsync",
            AdmissionRecoveryPorts.Request(present, "TicketRecovery"), CancellationToken.None);
        object absentResult = await AdmissionContractRuntime.InvokeAsync(
            AdmissionRecoveryPorts.Service(absent), "RequestAsync",
            AdmissionRecoveryPorts.Request(absent, "TicketRecovery"), CancellationToken.None);

        await AssertUniformReceiptAsync(presentResult);
        await AssertUniformReceiptAsync(absentResult);
        await Assert.That(AdmissionContractRuntime.PublicScalarSnapshot(presentResult))
            .IsEquivalentTo(AdmissionContractRuntime.PublicScalarSnapshot(absentResult));
        await Assert.That(present.RecoveryDeliveryCalls).IsEqualTo(1);
        await Assert.That(present.RecoveryByDigest.Values.Single().AdmissionTicketId)
            .IsEqualTo(present.CurrentAdmissionTicketId);
        await Assert.That(absent.RecoveryDeliveryCalls).IsEqualTo(0);
        await Assert.That(absent.RecoveryStoreCalls).IsEqualTo(0);
        await Assert.That(absent.RecoveryByDigest).IsEmpty();
        await Assert.That(absent.DigestIssueCalls).IsEqualTo(0);
    }

    private static async Task<AdmissionTestScenario> PresentScenarioAsync()
    {
        AdmissionTestScenario scenario = AdmissionTestScenario.Recovery(UtcNow, identityPresent: true);
        object issued = await AdmissionContractRuntime.InvokeAsync(
            AdmissionIssuancePorts.Service(scenario), "IssueConfirmedAsync",
            AdmissionIssuancePorts.Request(scenario), CancellationToken.None);
        await Assert.That(AdmissionContractRuntime.Ids(issued, "IssuedTicketIds").Single())
            .IsEqualTo(scenario.CurrentAdmissionTicketId);
        return scenario;
    }

    private static async Task AssertUniformReceiptAsync(object result)
    {
        string[] forbidden = ["Found", "Exists", "AdmissionTicketId", "Capability", "Credential"];
        string[] propertyNames = result.GetType().GetProperties().Select(property => property.Name).ToArray();
        await Assert.That(forbidden.Intersect(propertyNames, StringComparer.OrdinalIgnoreCase)).IsEmpty();
        await Assert.That(AdmissionContractRuntime.Outcome(result)).IsEqualTo("Accepted");
    }
}
