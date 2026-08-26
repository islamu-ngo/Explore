// ABOUTME: Specifies the Phase 21 online check-in, undo, batch, and scanner-capability Application contracts.
// ABOUTME: Uses strict reflection ports so absent production contracts compile as intentional RED failures.

using ApplicationUnitTests.Contracts.Admissions.Support;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ApplicationUnitTests.Contracts.Admissions;

public sealed class AdmissionCheckInOrchestrationRedTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    [Arguments("Staff")]
    [Arguments("ScannerCapability")]
    public async Task CheckInUsesOneTenantDigestLookupAndRequiresStaffOrScopedScannerAuthority(string authorityKind)
    {
        CheckInScenario scenario = new(UtcNow);
        Guid? scannerCapabilityId = null;
        Guid? staffActorId = scenario.StaffActorId;
        if (authorityKind == "ScannerCapability")
        {
            object issued = await IssueScannerCapabilityAsync(scenario);
            scannerCapabilityId = AdmissionContractRuntime.Value<Guid>(issued, "ScannerCapabilityId");
            staffActorId = null;
        }

        object result = await CheckInAsync(
            scenario,
            scenario.Credential,
            "CheckIn",
            staffActorId,
            scannerCapabilityId);

        await Assert.That(AdmissionContractRuntime.Outcome(result)).IsEqualTo("CheckedIn");
        await Assert.That(scenario.CredentialDigestCalls).IsEqualTo(1);
        await Assert.That(scenario.TenantDigestLookupCalls).IsEqualTo(1);
        await Assert.That(scenario.ObservedLookupTenantId).IsEqualTo(scenario.TenantId);
        await Assert.That(scenario.ObservedLookupDigests.Length).IsEqualTo(2);
        await Assert.That(scenario.ObservedLookupDigests[0]).IsEqualTo(scenario.Digest(scenario.Credential));
        await Assert.That(scenario.ObservedLookupDigests[1])
            .IsEqualTo(scenario.Digest($"{scenario.Credential}:retained"));
        await Assert.That(scenario.ObservedLookupEventId).IsEqualTo(scenario.EventId);
        await Assert.That(scenario.ObservedLookupTargetId).IsEqualTo(scenario.TargetId);
        await Assert.That(scenario.AuthorityChecks).IsEqualTo(1);
        await Assert.That(scenario.LastAuthorityKind).IsEqualTo(authorityKind);
        await Assert.That(scenario.AppendCount).IsEqualTo(1);
        await Assert.That(scenario.TelemetryCalls).Contains("RecordOperation");
    }

    [Test]
    [Arguments("WrongTenant")]
    [Arguments("WrongEvent")]
    [Arguments("WrongTarget")]
    [Arguments("RevokedCredential")]
    [Arguments("ExpiredCredential")]
    public async Task WrongLineageAndInactiveCredentialsReturnTheSameGenericOutcome(string rejection)
    {
        CheckInScenario scenario = new(UtcNow);
        Guid tenantId = rejection == "WrongTenant" ? Guid.CreateVersion7() : scenario.TenantId;
        Guid eventId = rejection == "WrongEvent" ? Guid.CreateVersion7() : scenario.EventId;
        Guid targetId = rejection == "WrongTarget" ? Guid.CreateVersion7() : scenario.TargetId;
        if (rejection == "RevokedCredential") scenario.CredentialState = "Revoked";
        if (rejection == "ExpiredCredential") scenario.CredentialState = "Expired";

        object result = await AdmissionContractRuntime.InvokeAsync(
            CheckInPorts.Service(scenario),
            "ProcessAsync",
            CheckInPorts.Request(
                scenario,
                scenario.Credential,
                "CheckIn",
                tenantId,
                eventId,
                targetId,
                scenario.StaffActorId,
                null),
            CancellationToken.None);

        await Assert.That(AdmissionContractRuntime.Outcome(result)).IsEqualTo("Rejected");
        await Assert.That(AdmissionContractRuntime.PublicScalarSnapshot(result).Keys)
            .DoesNotContain("AdmissionTicketId");
        await Assert.That(scenario.AppendCount).IsEqualTo(0);
    }

    [Test]
    public async Task ScannerCapabilityIssueAndRevokeRequireExactEventTargetLineage()
    {
        CheckInScenario scenario = new(UtcNow);
        object mismatchedIssue = await AdmissionContractRuntime.InvokeAsync(
            ScannerCapabilityPorts.Service(scenario),
            "IssueAsync",
            ScannerCapabilityPorts.IssueRequest(
                scenario,
                eventId: Guid.CreateVersion7()),
            CancellationToken.None);
        await Assert.That(AdmissionContractRuntime.Outcome(mismatchedIssue)).IsEqualTo("Rejected");
        await Assert.That(scenario.ScannerMaterialIssueCalls).IsEqualTo(0);
        await Assert.That(scenario.ScannerCapabilities).IsEmpty();

        scenario.TargetStopped = true;
        object stoppedIssue = await IssueScannerCapabilityAsync(scenario);
        await Assert.That(AdmissionContractRuntime.Outcome(stoppedIssue)).IsEqualTo("Rejected");
        await Assert.That(scenario.ScannerMaterialIssueCalls).IsEqualTo(0);
        scenario.TargetStopped = false;

        object issued = await IssueScannerCapabilityAsync(scenario);
        Guid capabilityId = AdmissionContractRuntime.Value<Guid>(issued, "ScannerCapabilityId");
        object mismatchedRevoke = await AdmissionContractRuntime.InvokeAsync(
            ScannerCapabilityPorts.Service(scenario),
            "RevokeAsync",
            ScannerCapabilityPorts.RevokeRequest(
                scenario,
                capabilityId,
                eventId: Guid.CreateVersion7()),
            CancellationToken.None);
        await Assert.That(AdmissionContractRuntime.Outcome(mismatchedRevoke)).IsEqualTo("Rejected");
        await Assert.That(scenario.ScannerCapabilities[capabilityId].RevokedAt).IsNull();
    }

    [Test]
    public async Task BatchRejectsMoreThanOneHundredAndReturnsIndependentOrderedPartialResults()
    {
        CheckInScenario overLimit = new(UtcNow);
        object tooLarge = await AdmissionContractRuntime.InvokeAsync(
            CheckInPorts.Service(overLimit),
            "ProcessBatchAsync",
            CheckInPorts.BatchRequest(
                overLimit,
                Enumerable.Range(0, 101).Select(_ => RuntimeCapability.New()).ToArray()),
            CancellationToken.None);

        await Assert.That(AdmissionContractRuntime.Outcome(tooLarge)).IsEqualTo("BatchLimitExceeded");
        await Assert.That(AdmissionContractRuntime.Items(tooLarge, "Items")).IsEmpty();
        await Assert.That(overLimit.TenantDigestLookupCalls).IsEqualTo(0);
        await Assert.That(overLimit.AppendCount).IsEqualTo(0);
        await Assert.That(overLimit.TelemetryCalls).Contains("RecordSaturation");

        CheckInScenario partial = new(UtcNow);
        string revoked = RuntimeCapability.New();
        string validSecond = RuntimeCapability.New();
        partial.CredentialStates[partial.Digest(revoked)] = "Revoked";
        partial.CredentialStates[partial.Digest(validSecond)] = "Active";
        string[] credentials = [partial.Credential, revoked, validSecond];
        object batch = await AdmissionContractRuntime.InvokeAsync(
            CheckInPorts.Service(partial),
            "ProcessBatchAsync",
            CheckInPorts.BatchRequest(partial, credentials),
            CancellationToken.None);
        object[] items = AdmissionContractRuntime.Items(batch, "Items");

        await Assert.That(AdmissionContractRuntime.Outcome(batch)).IsEqualTo("Completed");
        await Assert.That(items.Length).IsEqualTo(3);
        await Assert.That(string.Join(',', items.Select(AdmissionContractRuntime.Outcome)))
            .IsEqualTo("CheckedIn,Rejected,CheckedIn");
        await Assert.That(string.Join(',', items.Select(item => AdmissionContractRuntime.Value<int>(item, "Index"))))
            .IsEqualTo("0,1,2");
        await Assert.That(partial.AppendCount).IsEqualTo(2);
        await Assert.That(partial.TenantDigestLookupCalls).IsEqualTo(3);
        await Assert.That(partial.UnitOfWork.TransactionCount).IsEqualTo(3);
        await Assert.That(partial.TelemetryCalls).Contains("RecordBatch");
        await Assert.That(partial.TelemetryCalls.Count(call => call == "RecordOperation")).IsEqualTo(3);
    }

    [Test]
    public async Task BatchContinuesAfterOneIndependentInfrastructureFailure()
    {
        CheckInScenario scenario = new(UtcNow);
        string unavailable = RuntimeCapability.New();
        string validLast = RuntimeCapability.New();
        scenario.CredentialStates[scenario.Digest(unavailable)] = "Active";
        scenario.CredentialStates[scenario.Digest(validLast)] = "Active";
        scenario.UnavailableCredentialDigests.Add(scenario.Digest(unavailable));

        object batch = await AdmissionContractRuntime.InvokeAsync(
            CheckInPorts.Service(scenario),
            "ProcessBatchAsync",
            CheckInPorts.BatchRequest(
                scenario,
                [scenario.Credential, unavailable, validLast]),
            CancellationToken.None);
        object[] items = AdmissionContractRuntime.Items(batch, "Items");

        await Assert.That(string.Join(',', items.Select(AdmissionContractRuntime.Outcome)))
            .IsEqualTo("CheckedIn,Unavailable,CheckedIn");
        await Assert.That(items.Select(item =>
            AdmissionContractRuntime.Value<int>(item, "Index")).ToArray())
            .IsEquivalentTo([0, 1, 2]);
        await Assert.That(scenario.UnitOfWork.TransactionCount).IsEqualTo(3);
        await Assert.That(scenario.AppendCount).IsEqualTo(2);
    }

    [Test]
    public async Task DuplicateCheckInAndUndoAreDeterministicAndHistoryRemainsAppendOnly()
    {
        CheckInScenario scenario = new(UtcNow) { MaximumEntries = 2 };

        object first = await CheckInAsync(scenario, scenario.Credential, "CheckIn", scenario.StaffActorId, null);
        object duplicate = await CheckInAsync(scenario, scenario.Credential, "CheckIn", scenario.StaffActorId, null);
        object undo = await CheckInAsync(scenario, scenario.Credential, "Undo", scenario.StaffActorId, null);
        object duplicateUndo = await CheckInAsync(scenario, scenario.Credential, "Undo", scenario.StaffActorId, null);
        object reentry = await CheckInAsync(scenario, scenario.Credential, "CheckIn", scenario.StaffActorId, null);

        await Assert.That(string.Join(',', new[]
        {
            AdmissionContractRuntime.Outcome(first),
            AdmissionContractRuntime.Outcome(duplicate),
            AdmissionContractRuntime.Outcome(undo),
            AdmissionContractRuntime.Outcome(duplicateUndo),
            AdmissionContractRuntime.Outcome(reentry)
        })).IsEqualTo("CheckedIn,AlreadyCheckedIn,Undone,NotCheckedIn,CheckedIn");
        await Assert.That(string.Join(',', scenario.History.Select(value => value.Action)))
            .IsEqualTo("CheckIn,Undo,CheckIn");
        await Assert.That(string.Join(',', scenario.History.Select(value => value.Sequence)))
            .IsEqualTo("1,2,3");

        CheckInScenario singleEntry = new(UtcNow);
        _ = await CheckInAsync(singleEntry, singleEntry.Credential, "CheckIn", singleEntry.StaffActorId, null);
        _ = await CheckInAsync(singleEntry, singleEntry.Credential, "Undo", singleEntry.StaffActorId, null);
        object prohibitedReentry = await CheckInAsync(
            singleEntry, singleEntry.Credential, "CheckIn", singleEntry.StaffActorId, null);

        await Assert.That(AdmissionContractRuntime.Outcome(prohibitedReentry)).IsEqualTo("Rejected");
        await Assert.That(singleEntry.History.Count).IsEqualTo(2);
    }

    [Test]
    public async Task UndoRequiresTheExactActiveCheckInFactIdentity()
    {
        CheckInScenario scenario = new(UtcNow);
        object checkedIn = await CheckInAsync(
            scenario, scenario.Credential, "CheckIn", scenario.StaffActorId, null);
        Guid activeCheckInId = AdmissionContractRuntime.Value<Guid>(checkedIn, "CheckInId");

        object mismatched = await AdmissionContractRuntime.InvokeAsync(
            CheckInPorts.Service(scenario),
            "ProcessAsync",
            CheckInPorts.Request(
                scenario,
                scenario.Credential,
                "Undo",
                staffActorId: scenario.StaffActorId,
                checkInId: Guid.CreateVersion7()),
            CancellationToken.None);
        await Assert.That(AdmissionContractRuntime.Outcome(mismatched)).IsEqualTo("Rejected");
        await Assert.That(scenario.History).HasSingleItem();

        object exact = await AdmissionContractRuntime.InvokeAsync(
            CheckInPorts.Service(scenario),
            "ProcessAsync",
            CheckInPorts.Request(
                scenario,
                scenario.Credential,
                "Undo",
                staffActorId: scenario.StaffActorId,
                checkInId: activeCheckInId),
            CancellationToken.None);
        await Assert.That(AdmissionContractRuntime.Outcome(exact)).IsEqualTo("Undone");
        await Assert.That(scenario.History).HasCount(2);
    }

    [Test]
    public async Task ScannerCapabilityIsDisclosedOnceNeverReturnedByReadAndRevocationIsImmediate()
    {
        CheckInScenario scenario = new(UtcNow);
        object service = ScannerCapabilityPorts.Service(scenario);
        object issued = await AdmissionContractRuntime.InvokeAsync(
            service,
            "IssueAsync",
            ScannerCapabilityPorts.IssueRequest(scenario),
            CancellationToken.None);
        string plaintext = AdmissionContractRuntime.Value<string>(issued, "PlaintextCapability");
        Guid capabilityId = AdmissionContractRuntime.Value<Guid>(issued, "ScannerCapabilityId");
        object read = await AdmissionContractRuntime.InvokeAsync(
            service,
            "ReadAsync",
            ScannerCapabilityPorts.ReadRequest(scenario, capabilityId),
            CancellationToken.None);

        await Assert.That(string.IsNullOrWhiteSpace(plaintext)).IsFalse();
        await Assert.That(issued.ToString()).DoesNotContain(plaintext);
        await Assert.That(read.ToString()).DoesNotContain(plaintext);
        await Assert.That(read.GetType().GetProperties().Select(property => property.Name))
            .DoesNotContain("PlaintextCapability");
        await Assert.That(scenario.StoredScannerPlaintextCount).IsEqualTo(0);
        await Assert.That(scenario.ScannerMaterialIssueCalls).IsEqualTo(1);

        object duplicateIssue = await AdmissionContractRuntime.InvokeAsync(
            service,
            "IssueAsync",
            ScannerCapabilityPorts.IssueRequest(scenario),
            CancellationToken.None);
        await Assert.That(AdmissionContractRuntime.Outcome(duplicateIssue)).IsEqualTo("AlreadyIssued");
        await Assert.That(AdmissionContractRuntime.Value<Guid>(duplicateIssue, "ScannerCapabilityId"))
            .IsEqualTo(capabilityId);
        await Assert.That(duplicateIssue.GetType().GetProperty("PlaintextCapability")!.GetValue(duplicateIssue)).IsNull();
        await Assert.That(scenario.ScannerCapabilities.Count).IsEqualTo(1);

        object revoked = await AdmissionContractRuntime.InvokeAsync(
            service,
            "RevokeAsync",
            ScannerCapabilityPorts.RevokeRequest(scenario, capabilityId),
            CancellationToken.None);
        object denied = await CheckInAsync(scenario, scenario.Credential, "CheckIn", null, capabilityId);

        await Assert.That(AdmissionContractRuntime.Outcome(revoked)).IsEqualTo("Revoked");
        await Assert.That(AdmissionContractRuntime.Outcome(denied)).IsEqualTo("Rejected");
        await Assert.That(scenario.AppendCount).IsEqualTo(0);
    }

    [Test]
    [Arguments("WrongEvent")]
    [Arguments("WrongTarget")]
    [Arguments("Expired")]
    [Arguments("StolenFromOtherTenant")]
    public async Task ScannerCapabilityWrongScopeExpiryAndTheftFailWithOneGenericOutcome(string rejection)
    {
        CheckInScenario scenario = new(UtcNow);
        object issued = await IssueScannerCapabilityAsync(scenario);
        Guid capabilityId = AdmissionContractRuntime.Value<Guid>(issued, "ScannerCapabilityId");
        Guid tenantId = scenario.TenantId;
        Guid eventId = scenario.EventId;
        Guid targetId = scenario.TargetId;
        if (rejection == "WrongEvent") eventId = Guid.CreateVersion7();
        if (rejection == "WrongTarget") targetId = Guid.CreateVersion7();
        if (rejection == "Expired") scenario.Clock.Advance(TimeSpan.FromHours(2));
        if (rejection == "StolenFromOtherTenant") tenantId = Guid.CreateVersion7();

        object result = await AdmissionContractRuntime.InvokeAsync(
            CheckInPorts.Service(scenario),
            "ProcessAsync",
            CheckInPorts.Request(
                scenario,
                scenario.Credential,
                "CheckIn",
                tenantId,
                eventId,
                targetId,
                null,
                capabilityId),
            CancellationToken.None);

        await Assert.That(AdmissionContractRuntime.Outcome(result)).IsEqualTo("Rejected");
        await Assert.That(scenario.AppendCount).IsEqualTo(0);
    }

    [Test]
    public async Task DoorResultIsBoundedAndExcludesCredentialRosterOrderPaymentAndParticipantData()
    {
        CheckInScenario scenario = new(UtcNow);
        object result = await CheckInAsync(scenario, scenario.Credential, "CheckIn", scenario.StaffActorId, null);
        string[] forbiddenFragments =
        [
            "Credential", "Capability", "Digest", "Attendee", "Participant", "Email", "Order",
            "Payment", "Answer", "Address", "Phone", "Roster"
        ];
        PropertyInfo[] properties = result.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        await Assert.That(properties.Length).IsLessThanOrEqualTo(8);
        await Assert.That(properties.Select(property => property.Name).Any(name =>
            forbiddenFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase))))
            .IsFalse();
        await Assert.That(properties.Where(property => property.PropertyType == typeof(string))
            .Select(property => (string?)property.GetValue(result))
            .Where(value => value is not null)
            .All(value => value!.Length <= 128)).IsTrue();
        await Assert.That(result.ToString()).DoesNotContain(scenario.Credential);
    }

    [Test]
    public async Task RepositoryAndConnectivityErrorsFailClosedWithoutAdmissionSuccess()
    {
        CheckInScenario scenario = new(UtcNow) { FailRepository = true };
        object service = CheckInPorts.Service(scenario);
        object request = CheckInPorts.Request(scenario, scenario.Credential, "CheckIn");

        Exception exception = await Assert.ThrowsAsync<Exception>(async () =>
            await AdmissionContractRuntime.InvokeAsync(
                service, "ProcessAsync", request, CancellationToken.None));

        await Assert.That(exception.GetBaseException().GetType().Name)
            .IsEqualTo("AdmissionCheckInUnavailableException");
        await Assert.That(scenario.AppendCount).IsEqualTo(0);
    }

    [Test]
    public async Task CancellationTokenPropagatesToAuthorityDigestAndRepositoryWithoutWriting()
    {
        CheckInScenario scenario = new(UtcNow);
        object service = CheckInPorts.Service(scenario);
        object request = CheckInPorts.Request(scenario, scenario.Credential, "CheckIn");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await AdmissionContractRuntime.InvokeAsync(
                service,
                "ProcessAsync",
                request,
                cancellation.Token));

        await Assert.That(scenario.ObservedCancellationToken).IsEqualTo(cancellation.Token);
        await Assert.That(scenario.AppendCount).IsEqualTo(0);
    }

    private static Task<object> CheckInAsync(
        CheckInScenario scenario,
        string credential,
        string action,
        Guid? staffActorId,
        Guid? scannerCapabilityId) => AdmissionContractRuntime.InvokeAsync(
            CheckInPorts.Service(scenario),
            "ProcessAsync",
            CheckInPorts.Request(
                scenario,
                credential,
                action,
                scenario.TenantId,
                scenario.EventId,
                scenario.TargetId,
                staffActorId,
                scannerCapabilityId),
            CancellationToken.None);

    private static async Task<object> IssueScannerCapabilityAsync(CheckInScenario scenario) =>
        await AdmissionContractRuntime.InvokeAsync(
            ScannerCapabilityPorts.Service(scenario),
            "IssueAsync",
            ScannerCapabilityPorts.IssueRequest(scenario),
            CancellationToken.None);
}

internal static class CheckInPorts
{
    internal const string TransactionPort = "IAdmissionCheckInTransaction";
    internal const string DigestPort = "IAdmissionCheckInCredentialDigestService";
    internal const string AuthorityPort = "IAdmissionCheckInAuthority";
    internal const string TelemetryPort = "IAdmissionCheckInTelemetry";

    internal static object Service(CheckInScenario scenario) => AdmissionContractRuntime.Service(
        "AdmissionCheckInService",
        scenario.Clock,
        scenario.UnitOfWork,
        (TransactionPort, CheckInPortFake.Create(TransactionPort, scenario)),
        (DigestPort, CheckInPortFake.Create(DigestPort, scenario)),
        (AuthorityPort, CheckInPortFake.Create(AuthorityPort, scenario)),
        (TelemetryPort, CheckInPortFake.Create(TelemetryPort, scenario)));

    internal static object Request(
        CheckInScenario scenario,
        string credential,
        string action,
        Guid? tenantId = null,
        Guid? eventId = null,
        Guid? targetId = null,
        Guid? staffActorId = null,
        Guid? scannerCapabilityId = null,
        Guid? checkInId = null) => AdmissionContractRuntime.ApplicationObject(
            "AdmissionCheckInRequest",
            ("TenantId", tenantId ?? scenario.TenantId),
            ("EventId", eventId ?? scenario.EventId),
            ("TargetId", targetId ?? scenario.TargetId),
            ("Credential", credential),
            ("Action", action),
            ("ReasonCode", action == "Undo" ? "OperatorCorrection" : null),
            ("StaffActorId", staffActorId ?? (scannerCapabilityId is null ? scenario.StaffActorId : null)),
            ("ScannerCapabilityId", scannerCapabilityId),
            ("CheckInId", checkInId ?? (action == "Undo"
                ? scenario.History.LastOrDefault(fact => fact.Action == "CheckIn")?.Id
                : null)));

    internal static object BatchRequest(CheckInScenario scenario, IReadOnlyList<string> credentials)
    {
        Type itemType = AdmissionContractRuntime.ApplicationType("AdmissionCheckInBatchItem");
        object[] items = credentials.Select((credential, index) => AdmissionContractRuntime.Create(
            itemType,
            ("Index", index),
            ("Credential", credential),
            ("Action", "CheckIn"),
            ("ReasonCode", null))).ToArray();
        return AdmissionContractRuntime.ApplicationObject(
            "AdmissionCheckInBatchRequest",
            ("TenantId", scenario.TenantId),
            ("EventId", scenario.EventId),
            ("TargetId", scenario.TargetId),
            ("StaffActorId", scenario.StaffActorId),
            ("ScannerCapabilityId", null),
            ("Items", items));
    }
}

internal static class ScannerCapabilityPorts
{
    internal const string RepositoryPort = "IAdmissionScannerCapabilityRepository";
    internal const string MaterialPort = "IAdmissionScannerCapabilityMaterialService";

    internal static object Service(CheckInScenario scenario) => AdmissionContractRuntime.Service(
        "AdmissionScannerCapabilityService",
        scenario.Clock,
        scenario.UnitOfWork,
        (RepositoryPort, CheckInPortFake.Create(RepositoryPort, scenario)),
        (MaterialPort, CheckInPortFake.Create(MaterialPort, scenario)));

    internal static object IssueRequest(
        CheckInScenario scenario,
        Guid? eventId = null,
        Guid? targetId = null) => AdmissionContractRuntime.ApplicationObject(
        "AdmissionScannerCapabilityIssueRequest",
        ("IssueRequestId", scenario.ScannerCapabilityIssueRequestId),
        ("TenantId", scenario.TenantId),
        ("EventId", eventId ?? scenario.EventId),
        ("TargetId", targetId ?? scenario.TargetId),
        ("Actions", new[] { "CheckIn", "Undo" }),
        ("DeviceLabel", "North entrance scanner"),
        ("ExpiresAtUtc", scenario.Clock.GetUtcNow().AddHours(1)),
        ("IssuedByActorId", scenario.StaffActorId));

    internal static object ReadRequest(CheckInScenario scenario, Guid capabilityId) =>
        AdmissionContractRuntime.ApplicationObject(
            "AdmissionScannerCapabilityReadRequest",
            ("TenantId", scenario.TenantId),
            ("ScannerCapabilityId", capabilityId));

    internal static object RevokeRequest(
        CheckInScenario scenario,
        Guid capabilityId,
        Guid? eventId = null) =>
        AdmissionContractRuntime.ApplicationObject(
            "AdmissionScannerCapabilityRevokeRequest",
            ("TenantId", scenario.TenantId),
            ("EventId", eventId ?? scenario.EventId),
            ("ScannerCapabilityId", capabilityId),
            ("RevokedByActorId", scenario.StaffActorId),
            ("Reason", "DeviceLost"));
}

internal sealed class CheckInScenario
{
    internal CheckInScenario(DateTime utcNow)
    {
        Clock = new CheckInTimeProvider(utcNow);
        UnitOfWork = new CheckInUnitOfWork();
        CredentialStates[Digest(Credential)] = "Active";
    }

    internal Guid TenantId { get; } = Guid.CreateVersion7();
    internal Guid EventId { get; } = Guid.CreateVersion7();
    internal Guid TargetId { get; } = Guid.CreateVersion7();
    internal Guid AdmissionTicketId { get; } = Guid.CreateVersion7();
    internal Guid StaffActorId { get; } = Guid.CreateVersion7();
    internal Guid ScannerCapabilityIssueRequestId { get; } = Guid.CreateVersion7();
    internal string Credential { get; } = RuntimeCapability.New();
    internal CheckInTimeProvider Clock { get; }
    internal CheckInUnitOfWork UnitOfWork { get; }
    internal Dictionary<string, string> CredentialStates { get; } = new(StringComparer.Ordinal);
    internal HashSet<string> UnavailableCredentialDigests { get; } = new(StringComparer.Ordinal);
    internal Dictionary<Guid, AdmissionScannerCapability> ScannerCapabilities { get; } = [];
    internal List<CheckInFact> History { get; } = [];
    internal List<string> TelemetryCalls { get; } = [];
    internal string CredentialState
    {
        set => CredentialStates[Digest(Credential)] = value;
    }
    internal bool FailRepository { get; set; }
    internal bool TargetStopped { get; set; }
    internal int CredentialDigestCalls { get; set; }
    internal int TenantDigestLookupCalls { get; set; }
    internal int AuthorityChecks { get; set; }
    internal int ScannerMaterialIssueCalls { get; set; }
    internal Guid ObservedLookupTenantId { get; set; }
    internal string[] ObservedLookupDigests { get; set; } = [];
    internal Guid ObservedLookupEventId { get; set; }
    internal Guid ObservedLookupTargetId { get; set; }
    internal int MaximumEntries { get; set; } = 1;
    internal string? LastAuthorityKind { get; set; }
    internal CancellationToken ObservedCancellationToken { get; set; }
    internal int AppendCount => History.Count;
    internal int StoredScannerPlaintextCount => ScannerCapabilities.Values.Count(value =>
        value.GetType().GetProperties().Any(property =>
            property.Name.Contains("Plaintext", StringComparison.OrdinalIgnoreCase) &&
            property.GetValue(value) is string text && text.Length > 0));

    internal string Digest(string plaintext) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes($"{TenantId:N}:{plaintext}")));
}

internal sealed record CheckInFact(Guid Id, int Sequence, string Action, string CredentialDigest);
internal sealed class CheckInTimeProvider(DateTime utcNow) : TimeProvider
{
    private DateTimeOffset now = new(utcNow);
    public override DateTimeOffset GetUtcNow() => now;
    internal void Advance(TimeSpan duration) => now = now.Add(duration);
}

internal sealed class CheckInUnitOfWork : IUnitOfWork
{
    internal int TransactionCount { get; private set; }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
    {
        TransactionCount++;
        return operation(ct);
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        TransactionCount++;
        return operation(ct);
    }

    public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) =>
        operation(ct);
}

internal class CheckInPortFake : DispatchProxy
{
    private string portName = null!;
    private CheckInScenario scenario = null!;

    internal static object Create(string portName, CheckInScenario scenario)
    {
        Type port = AdmissionContractRuntime.ApplicationType(portName);
        object proxy = Create(port, typeof(CheckInPortFake));
        CheckInPortFake fake = (CheckInPortFake)proxy;
        fake.portName = portName;
        fake.scenario = scenario;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        MethodInfo method = targetMethod ?? throw AdmissionContractRuntime.Missing($"{portName} method");
        object?[] arguments = args ?? [];
        CancellationToken token = arguments.OfType<CancellationToken>().SingleOrDefault();
        scenario.ObservedCancellationToken = token;
        token.ThrowIfCancellationRequested();
        object request = arguments.First(value => value is not CancellationToken)!;
        return (portName, method.Name) switch
        {
            (CheckInPorts.DigestPort, "DigestAsync") => Digest(method.ReturnType, request),
            (CheckInPorts.AuthorityPort, "AuthorizeAsync") => Authorize(method.ReturnType, request),
            (CheckInPorts.TransactionPort, "ExecuteAsync") => Execute(method.ReturnType, request),
            (CheckInPorts.TelemetryPort, _) => RecordTelemetry(method.Name),
            (ScannerCapabilityPorts.MaterialPort, "IssueAsync") => IssueMaterial(method.ReturnType, request),
            (ScannerCapabilityPorts.RepositoryPort, "StoreAsync") => StoreCapability(method.ReturnType, request),
            (ScannerCapabilityPorts.RepositoryPort, "GetAsync") => GetCapability(method.ReturnType, arguments),
            (ScannerCapabilityPorts.RepositoryPort, "FindPlatformManagedTargetAsync") =>
                FindTarget(method.ReturnType, arguments),
            (ScannerCapabilityPorts.RepositoryPort, "UpdateAsync") => UpdateCapability(method.ReturnType, request),
            _ => throw AdmissionContractRuntime.Missing($"planned {portName}.{method.Name}")
        };
    }

    private object? RecordTelemetry(string methodName)
    {
        scenario.TelemetryCalls.Add(methodName);
        return null;
    }

    private object? Digest(Type returnType, object request)
    {
        _ = AdmissionContractRuntime.ExactObject(request, "AdmissionCheckInCredentialDigestRequest");
        scenario.CredentialDigestCalls++;
        string plaintext = AdmissionContractRuntime.Value<string>(request, "Credential");
        object current = AdmissionContractRuntime.ApplicationObject(
            "AdmissionCheckInCredentialDigestCandidate",
            ("LookupDigest", scenario.Digest(plaintext)),
            ("KeyVersion", 7));
        object retained = AdmissionContractRuntime.ApplicationObject(
            "AdmissionCheckInCredentialDigestCandidate",
            ("LookupDigest", scenario.Digest($"{plaintext}:retained")),
            ("KeyVersion", 6));
        object payload = Payload(returnType, "AdmissionCheckInCredentialDigest",
            ("Candidates", new[] { current, retained }));
        return AdmissionContractRuntime.WrapAsync(returnType, payload);
    }

    private object? Authorize(Type returnType, object request)
    {
        _ = AdmissionContractRuntime.ExactObject(request, "AdmissionCheckInAuthorizationRequest");
        scenario.AuthorityChecks++;
        Guid tenantId = AdmissionContractRuntime.Value<Guid>(request, "TenantId");
        Guid eventId = AdmissionContractRuntime.Value<Guid>(request, "EventId");
        Guid targetId = AdmissionContractRuntime.Value<Guid>(request, "TargetId");
        string action = AdmissionContractRuntime.Value<object>(request, "Action").ToString()!;
        Guid? actorId = OptionalValue<Guid>(request, "StaffActorId");
        Guid? scannerCapabilityId = OptionalValue<Guid>(request, "ScannerCapabilityId");
        bool authorized;
        if (actorId is not null)
        {
            scenario.LastAuthorityKind = "Staff";
            authorized = actorId == scenario.StaffActorId && tenantId == scenario.TenantId;
        }
        else
        {
            scenario.LastAuthorityKind = "ScannerCapability";
            AdmissionScannerCapability? row = scannerCapabilityId.HasValue &&
                scenario.ScannerCapabilities.TryGetValue(scannerCapabilityId.Value, out AdmissionScannerCapability? found)
                    ? found
                    : null;
            AdmissionScannerCapabilityAction domainAction = action == "CheckIn"
                ? AdmissionScannerCapabilityAction.CheckIn
                : AdmissionScannerCapabilityAction.Undo;
            authorized = row is not null && row.TenantId == tenantId && row.EventId == eventId &&
                         row.Permits(targetId, domainAction, scenario.Clock.GetUtcNow().UtcDateTime);
        }
        object payload = Payload(returnType, "AdmissionCheckInAuthorizationDecision",
            ("Outcome", authorized ? "Authorized" : "Denied"));
        return AdmissionContractRuntime.WrapAsync(returnType, payload);
    }

    private object? Execute(Type returnType, object request)
    {
        _ = AdmissionContractRuntime.ExactObject(request, "AdmissionCheckInTransactionRequest");
        scenario.TenantDigestLookupCalls++;
        Guid tenantId = AdmissionContractRuntime.Value<Guid>(request, "TenantId");
        Guid eventId = AdmissionContractRuntime.Value<Guid>(request, "EventId");
        Guid targetId = AdmissionContractRuntime.Value<Guid>(request, "TargetId");
        string[] digests = AdmissionContractRuntime.Items(request, "CredentialDigestCandidates")
            .Select(candidate => AdmissionContractRuntime.Value<string>(candidate, "LookupDigest"))
            .ToArray();
        string action = AdmissionContractRuntime.Value<object>(request, "Action").ToString()!;
        Guid? requestedCheckInId = OptionalValue<Guid>(request, "CheckInId");
        scenario.ObservedLookupTenantId = tenantId;
        scenario.ObservedLookupEventId = eventId;
        scenario.ObservedLookupTargetId = targetId;
        scenario.ObservedLookupDigests = digests;
        if (digests.Any(scenario.UnavailableCredentialDigests.Contains))
            throw new TimeoutException("simulated per-item admission repository outage");
        if (scenario.FailRepository) throw new TimeoutException("simulated admission repository outage");
        bool validLineage = tenantId == scenario.TenantId && eventId == scenario.EventId && targetId == scenario.TargetId;
        string? matchedDigest = digests.SingleOrDefault(digest =>
            scenario.CredentialStates.TryGetValue(digest, out string? state) && state == "Active");
        bool activeCredential = matchedDigest is not null;
        CheckInFact? latest = scenario.History.LastOrDefault(fact => fact.CredentialDigest == matchedDigest);
        bool currentlyCheckedIn = latest?.Action == "CheckIn";
        int priorEntries = scenario.History.Count(fact =>
            fact.CredentialDigest == matchedDigest && fact.Action == "CheckIn");
        AdmissionCheckInResultCodeEnum? resultCode;
        if (!validLineage || !activeCredential) resultCode = null;
        else if (action == "CheckIn" && currentlyCheckedIn)
            resultCode = AdmissionCheckInResultCodeEnum.AlreadyCheckedIn;
        else if (action == "Undo" && !currentlyCheckedIn)
            resultCode = AdmissionCheckInResultCodeEnum.NotCheckedIn;
        else if (action == "Undo" && requestedCheckInId != latest!.Id)
            resultCode = null;
        else if (action == "CheckIn" && priorEntries >= scenario.MaximumEntries) resultCode = null;
        else
        {
            resultCode = action == "Undo"
                ? AdmissionCheckInResultCodeEnum.Undone
                : priorEntries == 0
                    ? AdmissionCheckInResultCodeEnum.CheckedIn
                    : AdmissionCheckInResultCodeEnum.ReEntered;
            scenario.History.Add(new CheckInFact(
                Guid.CreateVersion7(),
                scenario.History.Count + 1,
                action,
                matchedDigest!));
        }
        if (!resultCode.HasValue)
        {
            return AdmissionContractRuntime.WrapAsync(returnType, null);
        }

        int entryCount = scenario.History.Count(fact =>
            fact.CredentialDigest == matchedDigest && fact.Action == "CheckIn");
        bool active = scenario.History.LastOrDefault(fact =>
            fact.CredentialDigest == matchedDigest)?.Action == "CheckIn";
        AdmissionCheckInState state = AdmissionCheckInState.Rehydrate(
            Guid.CreateVersion7(),
            scenario.TenantId,
            scenario.AdmissionTicketId,
            targetId,
            active ? scenario.History[^1].Id : null,
            entryCount,
            active ? (entryCount * 2L) - 1L : entryCount * 2L,
            Guid.CreateVersion7());
        ConstructorInfo constructor = typeof(AdmissionCheckInDecision).GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(value => value.GetParameters().Length == 3);
        object decision = constructor.Invoke([resultCode.Value, null, state]);
        return AdmissionContractRuntime.WrapAsync(returnType, decision);
    }

    private object? IssueMaterial(Type returnType, object request)
    {
        _ = AdmissionContractRuntime.ExactObject(request, "AdmissionScannerCapabilityMaterialRequest");
        scenario.ScannerMaterialIssueCalls++;
        string plaintext = RuntimeCapability.New();
        object payload = Payload(returnType, "AdmissionScannerCapabilityMaterial",
            ("PlaintextCapability", plaintext),
            ("LookupDigest", scenario.Digest(plaintext)),
            ("KeyVersion", 7));
        return AdmissionContractRuntime.WrapAsync(returnType, payload);
    }

    private object? StoreCapability(Type returnType, object request)
    {
        AdmissionScannerCapability capability = request as AdmissionScannerCapability
            ?? throw AdmissionContractRuntime.Missing("AdmissionScannerCapability entity persistence");
        EnsureNoPlaintextProperty(capability);
        AdmissionScannerCapability? existing = scenario.ScannerCapabilities.Values.SingleOrDefault(
            row => row.IssueRequestId == capability.IssueRequestId);
        bool created = existing is null;
        AdmissionScannerCapability stored = existing ?? capability;
        if (created)
        {
            scenario.ScannerCapabilities.Add(stored.Id, stored);
        }

        object payload = Payload(returnType, "AdmissionScannerCapabilityStoreResult",
            ("Created", created),
            ("Capability", stored));
        return AdmissionContractRuntime.WrapAsync(returnType, payload);
    }

    private object? GetCapability(Type returnType, object?[] arguments)
    {
        Guid[] ids = arguments.OfType<Guid>().ToArray();
        Guid tenantId = ids[0];
        Guid capabilityId = ids[1];
        scenario.ScannerCapabilities.TryGetValue(capabilityId, out AdmissionScannerCapability? capability);
        if (capability?.TenantId != tenantId)
        {
            capability = null;
        }
        return AdmissionContractRuntime.WrapAsync(returnType, capability);
    }

    private object? FindTarget(Type returnType, object?[] arguments)
    {
        Guid tenantId = (Guid)arguments[0]!;
        Guid eventId = (Guid)arguments[1]!;
        Guid targetId = (Guid)arguments[2]!;
        AdmissionTarget? target =
            tenantId == scenario.TenantId &&
            eventId == scenario.EventId &&
            targetId == scenario.TargetId
                ? AdmissionTarget.Create(
                    scenario.TargetId,
                    scenario.TenantId,
                    scenario.EventId,
                    AdmissionTargetTypeEnum.Event,
                    null,
                    null)
                : null;
        if (scenario.TargetStopped)
        {
            target?.Stop();
        }
        return AdmissionContractRuntime.WrapAsync(returnType, target);
    }

    private object? UpdateCapability(Type returnType, object request)
    {
        AdmissionScannerCapability capability = request as AdmissionScannerCapability
            ?? throw AdmissionContractRuntime.Missing("AdmissionScannerCapability entity update");
        scenario.ScannerCapabilities[capability.Id] = capability;
        return AdmissionContractRuntime.WrapAsync(returnType, capability);
    }

    private static object Payload(Type returnType, string expectedName, params (string Name, object? Value)[] values)
    {
        Type payload = AdmissionContractRuntime.AsyncPayload(returnType)
            ?? throw AdmissionContractRuntime.Missing($"{expectedName} return");
        if (payload.Name != expectedName)
            throw AdmissionContractRuntime.Missing($"exact {expectedName} return");
        return AdmissionContractRuntime.Create(payload, values);
    }

    private static T? OptionalValue<T>(object owner, string propertyName)
        where T : struct
    {
        PropertyInfo property = owner.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw AdmissionContractRuntime.Missing($"exact property {owner.GetType().Name}.{propertyName}");
        object? value = property.GetValue(owner);
        return value is null ? null : (T)value;
    }

    private static T? OptionalReference<T>(object owner, string propertyName)
        where T : class
    {
        PropertyInfo property = owner.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw AdmissionContractRuntime.Missing($"exact property {owner.GetType().Name}.{propertyName}");
        return (T?)property.GetValue(owner);
    }

    private static void EnsureNoPlaintextProperty(object request)
    {
        if (request.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Any(property =>
                property.Name.Contains("Plaintext", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("Capability", StringComparison.OrdinalIgnoreCase)))
        {
            throw AdmissionContractRuntime.Missing("digest-only scanner capability persistence");
        }
    }
}
