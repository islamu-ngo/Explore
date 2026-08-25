// ABOUTME: Implements only the exact planned issuance repository, digest, and delivery port calls.
// ABOUTME: Unknown calls fail immediately instead of being fabricated by a catch-all proxy.

using System.Reflection;
using System.Security.Cryptography;
using Explore.Application.Contracts.Admissions;
using System.Text;

namespace ApplicationUnitTests.Contracts.Admissions.Support;

internal static class AdmissionIssuancePorts
{
    internal const string RepositoryPort = "IAdmissionIssuanceRepository";
    internal const string DigestPort = "IAdmissionCredentialDigestService";
    internal const string EnvelopePort = "IAdmissionDeliveryEnvelopeProtector";
    internal const string DeliveryPort = "IAdmissionDeliveryDispatcher";

    internal static object Service(AdmissionTestScenario scenario)
    {
        _ = AdmissionContractRuntime.ApplicationType("AdmissionIssuanceService");
        return AdmissionContractRuntime.Service(
            "AdmissionIssuanceService",
            scenario.Clock,
            scenario.UnitOfWork,
            (RepositoryPort, IssuanceRepositoryFake.Create(scenario)),
            (DigestPort, IssuanceDigestFake.Create(scenario)),
            (EnvelopePort, IssuanceEnvelopeFake.Create()),
            (DeliveryPort, IssuanceDeliveryFake.Create(scenario)));
    }

    internal static object Request(AdmissionTestScenario scenario) => AdmissionContractRuntime.ApplicationObject(
        "AdmissionIssuanceRequest",
        ("TenantId", scenario.TenantId),
        ("RegistrationOrderId", scenario.OrderId),
        ("FinalizationEffectId", scenario.FinalizationEffectId),
        ("Authority", scenario.Authority));
}

internal class IssuanceRepositoryFake : DispatchProxy
{
    private AdmissionTestScenario scenario = null!;

    internal static object Create(AdmissionTestScenario scenario)
    {
        Type port = AdmissionContractRuntime.ApplicationType(AdmissionIssuancePorts.RepositoryPort);
        object proxy = Create(port, typeof(IssuanceRepositoryFake));
        ((IssuanceRepositoryFake)proxy).scenario = scenario;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        MethodInfo method = targetMethod ?? throw AdmissionContractRuntime.Missing("issuance repository method");
        object?[] arguments = args ?? [];
        return method.Name switch
        {
            "LoadAsync" => Load(method.ReturnType, arguments.Single(value => value is not CancellationToken)!),
            "ReloadCommittedAsync" => Load(method.ReturnType, arguments.Single(value => value is not CancellationToken)!),
            "IssueAndScheduleDeliveryAsync" => Persist(method.ReturnType, arguments.Single(value =>
                value is not CancellationToken)!),
            _ => throw AdmissionContractRuntime.Missing($"planned {AdmissionIssuancePorts.RepositoryPort}.{method.Name}")
        };
    }

    private object? Load(Type returnType, object request)
    {
        _ = AdmissionContractRuntime.ExactObject(request, "AdmissionIssuanceRequest");
        Type assignmentType = AdmissionContractRuntime.ApplicationType("AdmissionAssignmentFact");
        object[] assignments = scenario.AssignmentFacts.Select((fact, index) => AdmissionContractRuntime.Create(
            assignmentType,
            ("OrderLine", fact.Line),
            ("Assignment", fact.Assignment),
            ("Participant", fact.Participant),
            ("EventTicketType", fact.TicketType),
            ("LineUnitMinor", scenario.Assignments[index].LineUnitMinor),
            ("RelevantLineTotalMinor", scenario.Assignments.Sum(value => value.LineUnitMinor)),
            ("IsAdmissionLine", scenario.Assignments[index].IsAdmissionLine))).ToArray();
        Type payloadType = ExactPayload(returnType, "AdmissionIssuanceContext");
        object context = AdmissionContractRuntime.Create(
            payloadType,
            ("TenantId", scenario.TenantId),
            ("EventId", scenario.EventId),
            ("RegistrationOrderId", scenario.OrderId),
            ("FinalizationEffectId", scenario.FinalizationEffectId),
            ("Authority", scenario.Authority),
            ("PaymentReconciled", scenario.PaymentReconciled),
            ("OrderConfirmed", scenario.Authority != "PaymentSucceeded"),
            ("Order", scenario.Order),
            ("TicketCatalogVersion", scenario.Catalog),
            ("Assignments", assignments),
            ("ExistingTickets", scenario.TicketsByAssignment.Values.ToArray()),
            ("DeliveryAddress", "attendee@example.test"),
            ("ExistingDeliveryIntents", scenario.DeliveryIntentsById.Values.ToArray()));
        return AdmissionContractRuntime.WrapAsync(returnType, context);
    }

    private object? Persist(Type returnType, object request)
    {
        _ = AdmissionContractRuntime.ExactObject(request, "AdmissionIssuancePersistenceRequest");
        object[] tickets = AdmissionContractRuntime.Items(request, "Tickets");
        object[] intents = AdmissionContractRuntime.Items(request, "DeliveryIntents");
        var issued = new List<Guid>();
        var existing = new List<Guid>();
        foreach (object ticket in tickets)
        {
            Guid assignmentId = AdmissionContractRuntime.Value<Guid>(ticket, "RegistrationTicketAssignmentId");
            if (scenario.TicketsByAssignment.TryAdd(assignmentId, ticket)) issued.Add(AdmissionContractRuntime.EntityId(ticket));
            else existing.Add(AdmissionContractRuntime.EntityId(scenario.TicketsByAssignment[assignmentId]));
        }
        scenario.IssuanceWriteCalls++;
        scenario.PersistedDeliveryIntentCount += intents.Length;
        foreach (object intent in intents)
        {
            Guid intentId = AdmissionContractRuntime.EntityId(intent);
            if (!scenario.PendingDeliveryIntentIds.Add(intentId) || !scenario.DeliveryIntentsById.TryAdd(intentId, intent))
                throw AdmissionContractRuntime.Missing("unique durable delivery intent identity");
        }
        scenario.AtomicTicketAndIntentWriteObserved |= scenario.UnitOfWork.InTransaction && tickets.Length == intents.Length;
        Type payloadType = ExactPayload(returnType, "AdmissionIssuanceResult");
        object result = AdmissionContractRuntime.Create(
            payloadType,
            ("Outcome", issued.Count == 0 ? AdmissionIssuanceOutcome.AlreadyIssued : AdmissionIssuanceOutcome.Issued),
            ("IssuedTicketIds", issued.ToArray()),
            ("ExistingTicketIds", existing.ToArray()),
            ("Tickets", scenario.TicketsByAssignment.Values.ToArray()),
            ("DeliveryIntentIds", intents.Select(AdmissionContractRuntime.EntityId).ToArray()));
        return AdmissionContractRuntime.WrapAsync(returnType, result);
    }

    private static Type ExactPayload(Type returnType, string expectedName)
    {
        Type payload = AdmissionContractRuntime.AsyncPayload(returnType)
            ?? throw AdmissionContractRuntime.Missing($"{expectedName} return");
        return payload.Name == expectedName
            ? payload
            : throw AdmissionContractRuntime.Missing($"exact {expectedName} return");
    }
}

internal class IssuanceDigestFake : DispatchProxy
{
    private AdmissionTestScenario scenario = null!;

    internal static object Create(AdmissionTestScenario scenario)
    {
        Type port = AdmissionContractRuntime.ApplicationType(AdmissionIssuancePorts.DigestPort);
        object proxy = Create(port, typeof(IssuanceDigestFake));
        ((IssuanceDigestFake)proxy).scenario = scenario;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        MethodInfo method = targetMethod ?? throw AdmissionContractRuntime.Missing("issuance digest method");
        if (method.Name != "CreateAsync")
            throw AdmissionContractRuntime.Missing($"planned {AdmissionIssuancePorts.DigestPort}.{method.Name}");
        _ = AdmissionContractRuntime.ExactObject(
            (args ?? []).Single(value => value is not CancellationToken)!, "AdmissionCredentialCreateRequest");
        scenario.DigestIssueCalls++;
        string plaintext = RuntimeCapability.New();
        string digest = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));
        Type payloadType = AdmissionContractRuntime.AsyncPayload(method.ReturnType)
            ?? throw AdmissionContractRuntime.Missing("AdmissionCredentialMaterial return");
        if (payloadType.Name != "AdmissionCredentialMaterial")
            throw AdmissionContractRuntime.Missing("exact AdmissionCredentialMaterial return");
        object result = AdmissionContractRuntime.Create(
            payloadType,
            ("PlaintextCredential", plaintext),
            ("LookupDigest", digest),
            ("KeyVersion", 7),
            ("CredentialVersion", 1));
        return AdmissionContractRuntime.WrapAsync(method.ReturnType, result);
    }
}

internal class IssuanceEnvelopeFake : DispatchProxy
{
    internal static object Create()
    {
        Type port = AdmissionContractRuntime.ApplicationType(AdmissionIssuancePorts.EnvelopePort);
        return Create(port, typeof(IssuanceEnvelopeFake));
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        MethodInfo method = targetMethod ?? throw AdmissionContractRuntime.Missing("issuance envelope method");
        object?[] arguments = args ?? [];
        if (method.Name == "Protect")
        {
            object envelope = arguments.Single()!;
            string recipient = AdmissionContractRuntime.Value<string>(envelope, "RecipientAddress");
            string plaintext = AdmissionContractRuntime.Value<string>(envelope, "PlaintextCredential");
            return AdmissionContractRuntime.Create(method.ReturnType,
                ("Ciphertext", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{recipient}\n{plaintext}"))),
                ("ProtectionVersion", 1));
        }
        if (method.Name == "Unprotect")
        {
            string[] values = Encoding.UTF8.GetString(Convert.FromBase64String((string)arguments[0]!)).Split('\n', 2);
            return AdmissionContractRuntime.ApplicationObject(
                "AdmissionCredentialDeliveryEnvelope",
                ("RecipientAddress", values[0]),
                ("PlaintextCredential", values[1]));
        }
        throw AdmissionContractRuntime.Missing($"planned {AdmissionIssuancePorts.EnvelopePort}.{method.Name}");
    }
}

internal class IssuanceDeliveryFake : DispatchProxy
{
    private AdmissionTestScenario scenario = null!;

    internal static object Create(AdmissionTestScenario scenario)
    {
        Type port = AdmissionContractRuntime.ApplicationType(AdmissionIssuancePorts.DeliveryPort);
        object proxy = Create(port, typeof(IssuanceDeliveryFake));
        ((IssuanceDeliveryFake)proxy).scenario = scenario;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        MethodInfo method = targetMethod ?? throw AdmissionContractRuntime.Missing("issuance delivery method");
        if (method.Name != "DispatchAsync")
            throw AdmissionContractRuntime.Missing($"planned {AdmissionIssuancePorts.DeliveryPort}.{method.Name}");
        object request = AdmissionContractRuntime.ExactObject(
            (args ?? []).Single(value => value is not CancellationToken)!, "AdmissionDeliveryDispatchRequest");
        Guid intentId = AdmissionContractRuntime.Value<Guid>(request, "DeliveryIntentId");
        AdmissionDeliveryOutcome outcome = AdmissionDeliveryOutcome.Delivered;
        AdmissionDeliveryFailure failure = AdmissionDeliveryFailure.None;
        try
        {
            scenario.RecordIssuanceDispatch(intentId);
            object intent = scenario.DeliveryIntentsById[intentId];
            intent.GetType().GetMethod("MarkRouted")!
                .Invoke(intent, [scenario.Clock.GetUtcNow().UtcDateTime]);
            intent.GetType().GetMethod("CompleteHandoff")!
                .Invoke(intent, [$"test:{intentId:N}", scenario.Clock.GetUtcNow().UtcDateTime]);
        }
        catch (AdmissionDeliveryUnavailableException)
        {
            outcome = AdmissionDeliveryOutcome.RecoverablePending;
            failure = AdmissionDeliveryFailure.RouteUnavailable;
        }
        Type? payloadType = AdmissionContractRuntime.AsyncPayload(method.ReturnType);
        object? result = payloadType is null ? null : AdmissionContractRuntime.Create(
            payloadType, ("Outcome", outcome), ("Failure", failure));
        return AdmissionContractRuntime.WrapAsync(method.ReturnType, result);
    }
}

internal static class RuntimeCapability
{
    internal static string New() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
