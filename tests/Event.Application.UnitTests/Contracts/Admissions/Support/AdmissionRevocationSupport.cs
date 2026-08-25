// ABOUTME: Defines the exact revocation repository port fake and immutable refund/cancellation matrix.
// ABOUTME: The fake applies only service-computed revoked and preserved ticket identities.

using System.Reflection;

namespace ApplicationUnitTests.Contracts.Admissions.Support;

internal sealed record AdmissionAllocationSeed(
    Guid LineId,
    bool IsAdmissionLine,
    long RefundedMinor,
    long RelevantLineTotalMinor);

internal sealed record AdmissionRevocationRow(
    IReadOnlyList<AdmissionAllocationSeed> Allocations,
    bool Cancellation,
    bool Invalid,
    Guid[] ExpectedRevoked,
    Guid[] ExpectedPreserved)
{
    private static readonly Guid FirstLine = Guid.Parse("018e4e5c-7f00-7000-8000-000000000101");
    private static readonly Guid SecondLine = Guid.Parse("018e4e5c-7f00-7000-8000-000000000102");
    private static readonly Guid AddOnLine = Guid.Parse("018e4e5c-7f00-7000-8000-000000000103");

    internal static AdmissionAssignmentSeed[] Assignments() =>
    [
        new(Guid.CreateVersion7(), FirstLine, Guid.CreateVersion7(), 500, true),
        new(Guid.CreateVersion7(), FirstLine, Guid.CreateVersion7(), 500, true),
        new(Guid.CreateVersion7(), SecondLine, Guid.CreateVersion7(), 500, true)
    ];

    internal static AdmissionRevocationRow For(string matrixCase, AdmissionTestScenario scenario)
    {
        Guid[] first = scenario.TicketIdsForLine(FirstLine);
        Guid[] second = scenario.TicketIdsForLine(SecondLine);
        Guid[] all = first.Concat(second).ToArray();
        return matrixCase switch
        {
            "full-relevant" => Row([new(FirstLine, true, 1_000, 1_000)], first, second),
            "one-relevant-partial" => Row([new(FirstLine, true, 999, 1_000)], [], all),
            "add-on-only" => Row([new(AddOnLine, false, 300, 0)], [], all),
            "mixed-full-ticket-and-addon" => Row(
                [new(FirstLine, true, 1_000, 1_000), new(AddOnLine, false, 300, 0)], first, second),
            "multiple-ticket-lines-partial" => Row(
                [new(FirstLine, true, 500, 1_000), new(SecondLine, true, 250, 500)], [], all),
            "zero-relevant" => Row([new(FirstLine, true, 0, 1_000)], [], all),
            "negative" => Row([new(FirstLine, true, -1, 1_000)], [], all, invalid: true),
            "over-allocation" => Row([new(FirstLine, true, 1_001, 1_000)], [], all, invalid: true),
            "cancellation" => new([], true, false, all, []),
            _ => throw new ArgumentOutOfRangeException(nameof(matrixCase), matrixCase, null)
        };
    }

    private static AdmissionRevocationRow Row(
        IReadOnlyList<AdmissionAllocationSeed> allocations,
        Guid[] revoked,
        Guid[] preserved,
        bool invalid = false) => new(allocations, false, invalid, revoked, preserved);
}

internal static class AdmissionRevocationPorts
{
    internal const string RepositoryPort = "IAdmissionRevocationRepository";

    internal static object Service(AdmissionTestScenario scenario)
    {
        _ = AdmissionContractRuntime.ApplicationType("AdmissionRevocationService");
        return AdmissionContractRuntime.Service(
            "AdmissionRevocationService",
            scenario.Clock,
            scenario.UnitOfWork,
            (RepositoryPort, RevocationRepositoryFake.Create(scenario)));
    }

    internal static object Request(AdmissionTestScenario scenario, AdmissionRevocationRow row)
    {
        Type allocationType = AdmissionContractRuntime.ApplicationType("AdmissionRefundAllocationFact");
        object[] allocations = row.Allocations.Select(value => AdmissionContractRuntime.Create(
            allocationType,
            ("OrderLineId", value.LineId),
            ("IsAdmissionLine", value.IsAdmissionLine),
            ("RefundedMinor", value.RefundedMinor),
            ("RelevantLineTotalMinor", value.RelevantLineTotalMinor))).ToArray();
        return AdmissionContractRuntime.ApplicationObject(
            "AdmissionRevocationRequest",
            ("TenantId", scenario.TenantId),
            ("RegistrationOrderId", scenario.OrderId),
            ("Reason", row.Cancellation ? "OrderCancellation" : "RefundReconciled"),
            ("RefundAllocations", allocations));
    }
}

internal sealed class RevocationRepositoryFake : DispatchProxy
{
    private AdmissionTestScenario scenario = null!;

    internal static object Create(AdmissionTestScenario scenario)
    {
        Type port = AdmissionContractRuntime.ApplicationType(AdmissionRevocationPorts.RepositoryPort);
        object proxy = Create(port, typeof(RevocationRepositoryFake));
        ((RevocationRepositoryFake)proxy).scenario = scenario;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        MethodInfo method = targetMethod ?? throw AdmissionContractRuntime.Missing("revocation repository method");
        object?[] arguments = args ?? [];
        return method.Name switch
        {
            "LoadAsync" => Load(method.ReturnType, arguments.Single(value => value is not CancellationToken)!),
            "ApplyAsync" => Apply(method.ReturnType, arguments.Single(value => value is not CancellationToken)!),
            _ => throw AdmissionContractRuntime.Missing($"planned {AdmissionRevocationPorts.RepositoryPort}.{method.Name}")
        };
    }

    private object? Load(Type returnType, object request)
    {
        _ = AdmissionContractRuntime.ExactObject(request, "AdmissionRevocationRequest");
        Type payloadType = ExactPayload(returnType, "AdmissionRevocationContext");
        object context = AdmissionContractRuntime.Create(
            payloadType,
            ("TenantId", scenario.TenantId),
            ("RegistrationOrderId", scenario.OrderId),
            ("Tickets", scenario.TicketsByAssignment.Values.ToArray()));
        return AdmissionContractRuntime.WrapAsync(returnType, context);
    }

    private object? Apply(Type returnType, object request)
    {
        _ = AdmissionContractRuntime.ExactObject(request, "AdmissionRevocationPersistenceRequest");
        Guid[] revoked = AdmissionContractRuntime.Ids(request, "RevokedTicketIds");
        Guid[] preserved = AdmissionContractRuntime.Ids(request, "PreservedTicketIds");
        scenario.RevocationWriteCalls++;
        Type payloadType = ExactPayload(returnType, "AdmissionRevocationResult");
        object result = AdmissionContractRuntime.Create(
            payloadType,
            ("Outcome", "Applied"),
            ("RevokedTicketIds", revoked),
            ("PreservedTicketIds", preserved));
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
