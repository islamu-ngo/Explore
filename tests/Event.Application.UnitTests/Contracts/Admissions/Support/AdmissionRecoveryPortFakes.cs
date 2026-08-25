// ABOUTME: Implements the exact recovery capability and delivery ports plus exact public request construction.
// ABOUTME: CSPRNG plaintext crosses only capability-to-delivery test edges and is never logged.

using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ApplicationUnitTests.Contracts.Admissions.Support;

internal static class AdmissionRecoveryPorts
{
    internal const string CapabilityPort = "IAdmissionRecoveryCapabilityService";
    internal const string DeliveryPort = "IAdmissionRecoveryDeliveryService";

    internal static object Service(AdmissionTestScenario scenario)
    {
        _ = AdmissionContractRuntime.ApplicationType("AdmissionRecoveryService");
        return AdmissionContractRuntime.Service(
            "AdmissionRecoveryService",
            scenario.Clock,
            scenario.UnitOfWork,
            (RecoveryRepositoryFake.PortName, RecoveryRepositoryFake.Create(scenario)),
            (CapabilityPort, RecoveryCapabilityFake.Create(scenario)),
            (DeliveryPort, RecoveryDeliveryFake.Create(scenario)));
    }

    internal static object Request(AdmissionTestScenario scenario, string purpose) =>
        AdmissionContractRuntime.ApplicationObject(
            "AdmissionRecoveryRequest",
            ("TenantId", scenario.TenantId),
            ("NormalizedIdentity", scenario.NormalizedIdentity),
            ("Purpose", purpose));

    internal static object Consume(
        AdmissionTestScenario scenario,
        string capability,
        string purpose,
        Guid tenantId) => AdmissionContractRuntime.ApplicationObject(
            "AdmissionRecoveryConsumeRequest",
            ("TenantId", tenantId),
            ("RecoveryRequestId", scenario.RecoveryRequestId),
            ("Capability", capability),
            ("Purpose", purpose));

    internal static object Resend(AdmissionTestScenario scenario) => AdmissionContractRuntime.ApplicationObject(
        "AdmissionRecoveryResendRequest",
        ("TenantId", scenario.TenantId),
        ("RecoveryRequestId", scenario.RecoveryRequestId),
        ("Purpose", "TicketRecovery"));
}

internal class RecoveryCapabilityFake : DispatchProxy
{
    private AdmissionTestScenario scenario = null!;

    internal static object Create(AdmissionTestScenario scenario)
    {
        Type port = AdmissionContractRuntime.ApplicationType(AdmissionRecoveryPorts.CapabilityPort);
        object proxy = Create(port, typeof(RecoveryCapabilityFake));
        ((RecoveryCapabilityFake)proxy).scenario = scenario;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        MethodInfo method = targetMethod ?? throw AdmissionContractRuntime.Missing("recovery capability method");
        object request = (args ?? []).Single(value => value is not CancellationToken)!;
        return method.Name switch
        {
            "IssueAsync" => Issue(method.ReturnType,
                AdmissionContractRuntime.ExactObject(request, "AdmissionRecoveryCapabilityIssueRequest")),
            "DigestAsync" => Digest(method.ReturnType,
                AdmissionContractRuntime.ExactObject(request, "AdmissionRecoveryCapabilityDigestRequest")),
            _ => throw AdmissionContractRuntime.Missing(
                $"planned {AdmissionRecoveryPorts.CapabilityPort}.{method.Name}")
        };
    }

    private object? Issue(Type returnType, object request)
    {
        RequireLineage(request);
        scenario.DigestIssueCalls++;
        string capability = RuntimeCapability.New();
        string digest = Digest(capability);
        Type payloadType = RequiredPayload(returnType, "AdmissionRecoveryCapabilityMaterial");
        object result = AdmissionContractRuntime.Create(
            payloadType,
            ("Capability", capability),
            ("LookupDigest", digest),
            ("KeyVersion", 7),
            ("Purpose", AdmissionContractRuntime.Value<object>(request, "Purpose")),
            ("ExpiresAtUtc", scenario.Clock.GetUtcNow().AddHours(1)));
        return AdmissionContractRuntime.WrapAsync(returnType, result);
    }

    private object? Digest(Type returnType, object request)
    {
        RequireLineage(request);
        string capability = AdmissionContractRuntime.Value<string>(request, "Capability");
        Type payloadType = RequiredPayload(returnType, "AdmissionRecoveryCapabilityDigest");
        object result = AdmissionContractRuntime.Create(
            payloadType, ("LookupDigest", Digest(capability)), ("KeyVersion", 7));
        return AdmissionContractRuntime.WrapAsync(returnType, result);
    }

    private static string Digest(string capability) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(capability)));

    private void RequireLineage(object request)
    {
        if (AdmissionContractRuntime.Value<Guid>(request, "TenantId") != scenario.TenantId ||
            AdmissionContractRuntime.Value<Guid>(request, "RecoveryRequestId") != scenario.RecoveryRequestId ||
            AdmissionContractRuntime.Value<Guid>(request, "AdmissionTicketId") != scenario.CurrentAdmissionTicketId ||
            AdmissionContractRuntime.Value<object>(request, "Purpose").ToString() != "TicketRecovery")
        {
            throw AdmissionContractRuntime.Missing("matching recovery capability lineage");
        }
    }

    private static Type RequiredPayload(Type returnType, string expectedName)
    {
        Type payload = AdmissionContractRuntime.AsyncPayload(returnType)
            ?? throw AdmissionContractRuntime.Missing($"{expectedName} return");
        if (payload.Name != expectedName)
            throw AdmissionContractRuntime.Missing($"exact {expectedName} return");
        return payload;
    }
}

internal class RecoveryDeliveryFake : DispatchProxy
{
    private AdmissionTestScenario scenario = null!;

    internal static object Create(AdmissionTestScenario scenario)
    {
        Type port = AdmissionContractRuntime.ApplicationType(AdmissionRecoveryPorts.DeliveryPort);
        object proxy = Create(port, typeof(RecoveryDeliveryFake));
        ((RecoveryDeliveryFake)proxy).scenario = scenario;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        MethodInfo method = targetMethod ?? throw AdmissionContractRuntime.Missing("recovery delivery method");
        if (method.Name != "DeliverAsync")
            throw AdmissionContractRuntime.Missing($"planned {AdmissionRecoveryPorts.DeliveryPort}.{method.Name}");
        object request = AdmissionContractRuntime.ExactObject(
            (args ?? []).Single(value => value is not CancellationToken)!, "AdmissionRecoveryDeliveryRequest");
        Guid tenantId = AdmissionContractRuntime.Value<Guid>(request, "TenantId");
        Guid requestId = AdmissionContractRuntime.Value<Guid>(request, "RecoveryRequestId");
        Guid ticketId = AdmissionContractRuntime.Value<Guid>(request, "AdmissionTicketId");
        string purpose = AdmissionContractRuntime.Value<object>(request, "Purpose").ToString()!;
        string capability = AdmissionContractRuntime.Value<string>(request, "Capability");
        string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(capability)));
        if (!scenario.IdentityPresent || tenantId != scenario.TenantId || requestId != scenario.RecoveryRequestId ||
            ticketId != scenario.CurrentAdmissionTicketId || purpose != "TicketRecovery" ||
            !scenario.RecoveryByDigest.TryGetValue(digest, out StoredRecoveryCapability? stored) ||
            stored.TenantId != tenantId || stored.RecoveryRequestId != requestId ||
            stored.AdmissionTicketId != ticketId || stored.Purpose != purpose || stored.Consumed || stored.Rotated)
        {
            throw AdmissionContractRuntime.Missing("stored current recovery record before delivery");
        }
        scenario.DeliverCapability(capability);
        Type? payloadType = AdmissionContractRuntime.AsyncPayload(method.ReturnType);
        object? result = payloadType is null ? null : AdmissionContractRuntime.Create(
            payloadType, ("Outcome", "Accepted"));
        return AdmissionContractRuntime.WrapAsync(method.ReturnType, result);
    }
}
