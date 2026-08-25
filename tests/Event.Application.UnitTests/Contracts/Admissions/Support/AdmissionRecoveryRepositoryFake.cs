// ABOUTME: Implements the exact recovery repository port with ticket-bound digest lineage and atomic rotation.
// ABOUTME: Every mutation rejects tenant, request, ticket, purpose, or digest mismatches and stores no plaintext.

using System.Reflection;

namespace ApplicationUnitTests.Contracts.Admissions.Support;

internal class RecoveryRepositoryFake : DispatchProxy
{
    internal const string PortName = "IAdmissionRecoveryRepository";
    private AdmissionTestScenario scenario = null!;

    internal static object Create(AdmissionTestScenario scenario)
    {
        Type port = AdmissionContractRuntime.ApplicationType(PortName);
        object proxy = Create(port, typeof(RecoveryRepositoryFake));
        ((RecoveryRepositoryFake)proxy).scenario = scenario;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        MethodInfo method = targetMethod ?? throw AdmissionContractRuntime.Missing("recovery repository method");
        object?[] arguments = args ?? [];
        return method.Name switch
        {
            "FindIdentityAsync" => FindIdentity(method.ReturnType, ExactRequest(arguments, "AdmissionRecoveryRequest")),
            "StoreAsync" => Store(method.ReturnType, ExactRequest(arguments, "AdmissionRecoveryCapabilityRecord")),
            "GetByDigestAsync" => GetByDigest(
                method.ReturnType, ExactRequest(arguments, "AdmissionRecoveryCapabilityLookup")),
            "GetCurrentByRequestIdAsync" => GetCurrent(method, arguments),
            "ConsumeAsync" => Consume(
                method.ReturnType, ExactRequest(arguments, "AdmissionRecoveryCapabilityMutation")),
            "RotateAsync" => Rotate(
                method.ReturnType, ExactRequest(arguments, "AdmissionRecoveryRotationRequest")),
            _ => throw AdmissionContractRuntime.Missing($"planned {PortName}.{method.Name}")
        };
    }

    private object? FindIdentity(Type returnType, object request)
    {
        Require(AdmissionContractRuntime.Value<Guid>(request, "TenantId") == scenario.TenantId, "identity tenant");
        Type payload = RequiredPayload(returnType, "AdmissionRecoveryIdentityResult");
        object result = AdmissionContractRuntime.Create(
            payload,
            ("TenantId", scenario.TenantId),
            ("RecoveryRequestId", scenario.RecoveryRequestId),
            ("IdentityPresent", scenario.IdentityPresent),
            ("AdmissionTicketIds", scenario.TicketsByAssignment.Values.Select(AdmissionContractRuntime.EntityId).ToArray()));
        return AdmissionContractRuntime.WrapAsync(returnType, result);
    }

    private object? Store(Type returnType, object request)
    {
        Require(scenario.IdentityPresent, "present identity before recovery storage");
        StoredRecoveryCapability value = RecordFrom(request, "LookupDigest", "ExpiresAtUtc");
        Require(!scenario.RecoveryByDigest.ContainsKey(value.Digest), "new recovery digest");
        scenario.RecoveryByDigest.Add(value.Digest, value);
        scenario.RecoveryStoreCalls++;
        return Success(returnType, "Stored");
    }

    private object? GetByDigest(Type returnType, object request)
    {
        Guid tenantId = AdmissionContractRuntime.Value<Guid>(request, "TenantId");
        Guid requestId = AdmissionContractRuntime.Value<Guid>(request, "RecoveryRequestId");
        Guid ticketId = AdmissionContractRuntime.Value<Guid>(request, "AdmissionTicketId");
        string purpose = AdmissionContractRuntime.Value<object>(request, "Purpose").ToString()!;
        string digest = AdmissionContractRuntime.Value<string>(request, "LookupDigest");
        RequireLineage(tenantId, requestId, ticketId, purpose);
        scenario.RecoveryByDigest.TryGetValue(digest, out StoredRecoveryCapability? stored);
        if (stored is not null) RequireStoredLineage(stored, tenantId, requestId, ticketId, purpose);
        return State(returnType, stored, digest, tenantId, requestId, ticketId, purpose);
    }

    private object? GetCurrent(MethodInfo method, object?[] arguments)
    {
        ParameterInfo[] parameters = method.GetParameters();
        Require(parameters.Length == 4 && parameters[0].ParameterType == typeof(Guid) &&
                parameters[1].ParameterType == typeof(Guid) && parameters[3].ParameterType == typeof(CancellationToken),
            "GetCurrentByRequestIdAsync(TenantId, RecoveryRequestId, Purpose, ct) signature");
        Guid tenantId = (Guid)arguments[0]!;
        Guid requestId = (Guid)arguments[1]!;
        string purpose = arguments[2]!.ToString()!;
        Require(tenantId == scenario.TenantId, "current recovery tenant");
        Require(requestId == scenario.RecoveryRequestId, "current recovery request ID");
        StoredRecoveryCapability stored = scenario.RecoveryByDigest.Values.Single(value =>
            !value.Consumed && !value.Rotated && value.RecoveryRequestId == requestId);
        RequireStoredLineage(stored, tenantId, requestId, scenario.CurrentAdmissionTicketId, purpose);
        scenario.RecoveryCurrentReadCalls++;
        return State(method.ReturnType, stored, stored.Digest, tenantId, requestId, stored.AdmissionTicketId, purpose);
    }

    private object? Consume(Type returnType, object request)
    {
        StoredRecoveryCapability requested = RecordFrom(request, "LookupDigest", "ExpiresAtUtc");
        StoredRecoveryCapability stored = scenario.RecoveryByDigest[requested.Digest];
        RequireStoredLineage(stored, requested.TenantId, requested.RecoveryRequestId,
            requested.AdmissionTicketId, requested.Purpose);
        Require(!stored.Consumed && !stored.Rotated, "current single-use recovery digest");
        scenario.RecoveryByDigest[stored.Digest] = stored with { Consumed = true };
        return Success(returnType, "Consumed");
    }

    private object? Rotate(Type returnType, object request)
    {
        Require(scenario.UnitOfWork.InTransaction, "atomic recovery rotation transaction");
        Guid tenantId = AdmissionContractRuntime.Value<Guid>(request, "TenantId");
        Guid requestId = AdmissionContractRuntime.Value<Guid>(request, "RecoveryRequestId");
        Guid ticketId = AdmissionContractRuntime.Value<Guid>(request, "AdmissionTicketId");
        string purpose = AdmissionContractRuntime.Value<object>(request, "Purpose").ToString()!;
        string oldDigest = AdmissionContractRuntime.Value<string>(request, "OldLookupDigest");
        string replacementDigest = AdmissionContractRuntime.Value<string>(request, "ReplacementLookupDigest");
        RequireLineage(tenantId, requestId, ticketId, purpose);
        StoredRecoveryCapability old = scenario.RecoveryByDigest[oldDigest];
        RequireStoredLineage(old, tenantId, requestId, ticketId, purpose);
        Require(!old.Consumed && !old.Rotated, "rotatable old recovery digest");
        Require(oldDigest != replacementDigest && !scenario.RecoveryByDigest.ContainsKey(replacementDigest),
            "distinct replacement recovery digest");
        DateTimeOffset expiry = AdmissionContractRuntime.Value<DateTimeOffset>(request, "ReplacementExpiresAtUtc");
        scenario.RecoveryByDigest[oldDigest] = old with { Rotated = true };
        scenario.RecoveryByDigest.Add(replacementDigest,
            new(tenantId, requestId, ticketId, replacementDigest, purpose, expiry, false, false));
        scenario.RecoveryRotationCalls++;
        return Success(returnType, "Rotated");
    }

    private StoredRecoveryCapability RecordFrom(object request, string digestProperty, string expiryProperty)
    {
        Guid tenantId = AdmissionContractRuntime.Value<Guid>(request, "TenantId");
        Guid requestId = AdmissionContractRuntime.Value<Guid>(request, "RecoveryRequestId");
        Guid ticketId = AdmissionContractRuntime.Value<Guid>(request, "AdmissionTicketId");
        string purpose = AdmissionContractRuntime.Value<object>(request, "Purpose").ToString()!;
        RequireLineage(tenantId, requestId, ticketId, purpose);
        return new(tenantId, requestId, ticketId,
            AdmissionContractRuntime.Value<string>(request, digestProperty), purpose,
            AdmissionContractRuntime.Value<DateTimeOffset>(request, expiryProperty), false, false);
    }

    private void RequireLineage(Guid tenantId, Guid requestId, Guid ticketId, string purpose)
    {
        Require(tenantId == scenario.TenantId, "recovery tenant");
        Require(requestId == scenario.RecoveryRequestId, "recovery request ID");
        Require(ticketId == scenario.CurrentAdmissionTicketId, "issued admission ticket");
        Require(purpose == "TicketRecovery", "recovery purpose");
    }

    private static void RequireStoredLineage(
        StoredRecoveryCapability stored, Guid tenantId, Guid requestId, Guid ticketId, string purpose) =>
        Require(stored.TenantId == tenantId && stored.RecoveryRequestId == requestId &&
                stored.AdmissionTicketId == ticketId && stored.Purpose == purpose, "stored recovery lineage");

    private static object ExactRequest(object?[] arguments, string name) =>
        AdmissionContractRuntime.ExactObject(arguments.Single(value => value is not CancellationToken)!, name);

    private static object? State(Type returnType, StoredRecoveryCapability? stored, string digest,
        Guid tenantId, Guid requestId, Guid ticketId, string purpose)
    {
        Type payload = RequiredPayload(returnType, "AdmissionRecoveryCapabilityState");
        object result = AdmissionContractRuntime.Create(payload,
            ("Found", stored is not null), ("TenantId", tenantId), ("RecoveryRequestId", requestId),
            ("AdmissionTicketId", ticketId), ("LookupDigest", digest), ("Purpose", purpose),
            ("ExpiresAtUtc", stored?.ExpiresAtUtc ?? default), ("Consumed", stored?.Consumed ?? false),
            ("Rotated", stored?.Rotated ?? false));
        return AdmissionContractRuntime.WrapAsync(returnType, result);
    }

    private static Type RequiredPayload(Type returnType, string name)
    {
        Type payload = AdmissionContractRuntime.AsyncPayload(returnType)
            ?? throw AdmissionContractRuntime.Missing($"{name} return");
        return payload.Name == name ? payload : throw AdmissionContractRuntime.Missing($"exact {name} return");
    }

    private static object? Success(Type returnType, string outcome)
    {
        Type? payload = AdmissionContractRuntime.AsyncPayload(returnType);
        object? result = payload is null ? null : AdmissionContractRuntime.Create(payload, ("Outcome", outcome));
        return AdmissionContractRuntime.WrapAsync(returnType, result);
    }

    private static void Require(bool condition, string fact)
    {
        if (!condition) throw AdmissionContractRuntime.Missing($"matching {fact}");
    }
}
