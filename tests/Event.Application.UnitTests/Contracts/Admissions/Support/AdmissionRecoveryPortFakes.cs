// ABOUTME: Implements the exact recovery capability and delivery ports plus exact public request construction.
// ABOUTME: CSPRNG plaintext crosses only capability-to-delivery test edges and is never logged.

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Admissions;
using Explore.Application.Services.Registration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApplicationUnitTests.Contracts.Admissions.Support;

internal static class AdmissionRecoveryPorts
{
    internal const string CapabilityPort = "IAdmissionRecoveryCapabilityService";
    internal const string DeliveryStagerPort = "IAdmissionRecoveryDeliveryStager";

    internal static object Service(AdmissionTestScenario scenario)
    {
        _ = AdmissionContractRuntime.ApplicationType("AdmissionRecoveryService");
        return AdmissionContractRuntime.Service(
            "AdmissionRecoveryService",
            scenario.Clock,
            scenario.UnitOfWork,
            (RecoveryRepositoryFake.PortName, new RecoveryRepositoryFake(scenario)),
            (RecoveryIdentityResolverFake.PortName, new RecoveryIdentityResolverFake(scenario)),
            (CapabilityPort, RecoveryCapabilityFake.Create(scenario)),
            (DeliveryStagerPort, new RecoveryDeliveryStagerFake(scenario)),
            (nameof(IAdmissionRecoveryAuditService), new RecoveryAuditFake()),
            (nameof(IAdmissionRecoveryRateLimiter), new RecoveryRateLimiterFake()),
            (nameof(IAdmissionRecoveryRequestStager), new RecoveryRequestStagerFake(scenario)),
            ("ILogger`1", NullLogger<AdmissionRecoveryService>.Instance));
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

internal sealed class RecoveryDeliveryStagerFake(AdmissionTestScenario scenario) :
    IAdmissionRecoveryDeliveryStager
{
    public Task<AdmissionRecoveryDeliveryResult> StageAsync(
        AdmissionRecoveryDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        if (!scenario.UnitOfWork.InTransaction)
        {
            throw AdmissionContractRuntime.Missing("protected recovery staging transaction");
        }

        scenario.DeliverCapability(request.Capability);
        return Task.FromResult(
            new AdmissionRecoveryDeliveryResult(AdmissionRecoveryDeliveryOutcome.Accepted));
    }
}

internal sealed class RecoveryAuditFake : IAdmissionRecoveryAuditService
{
    public Task AppendAsync(
        AdmissionRecoveryAuditFact fact,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

internal sealed class RecoveryRateLimiterFake : IAdmissionRecoveryRateLimiter
{
    public AdmissionRecoveryRateLimitDecision TryAcquire(
        Guid tenantId,
        string normalizedIdentity,
        DateTimeOffset occurredAtUtc) =>
        new(true);
}

internal sealed class RecoveryRequestStagerFake(AdmissionTestScenario scenario) :
    IAdmissionRecoveryRequestStager
{
    public Task StageAsync(
        Guid tenantId,
        AdmissionRecoveryRequestEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (tenantId != scenario.TenantId ||
            envelope.Purpose != AdmissionRecoveryPurpose.TicketRecovery ||
            envelope.NormalizedIdentity != scenario.NormalizedIdentity.ToUpperInvariant())
        {
            throw AdmissionContractRuntime.Missing("normalized encrypted recovery request staging");
        }

        scenario.RecoveryRequestStageCalls++;
        if (scenario.FailRecoveryRequestStaging)
        {
            throw new InvalidOperationException("simulated durable staging failure");
        }

        return Task.CompletedTask;
    }
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
            ("LocatorDigest", digest),
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
