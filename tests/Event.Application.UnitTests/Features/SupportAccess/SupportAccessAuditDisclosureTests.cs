// ABOUTME: Pins the tenant-ownership check that keeps support-access audit evidence inside its own tenant.
// ABOUTME: The repository read is deliberately session-scoped only, so this handler check is the whole boundary.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.SupportAccess.Handlers.Queries;
using Explore.Application.Features.SupportAccess.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace ApplicationUnitTests.Features.SupportAccess;

/// <summary>
/// Support-access audit evidence is what a tenant relies on to hold the operator accountable, so it must
/// reach exactly that tenant.
/// <para>
/// The boundary is unusual and worth pinning explicitly: `ISupportAccessAuditEventRepository.ListForSessionAsync`
/// filters by session id alone — no tenant predicate — and `SupportAccessAuditEvent` carries `TargetTenantId`
/// rather than `TenantId`, so the global tenant query filter does not apply to it either. The handler's
/// ownership check is therefore the entire isolation boundary. Nothing beneath it would catch a mistake.
/// </para>
/// </summary>
public sealed class SupportAccessAuditDisclosureTests
{
    private readonly ISupportAccessSessionRepository _sessionRepository =
        Substitute.For<ISupportAccessSessionRepository>();

    private readonly ISupportAccessAuditEventRepository _auditEventRepository =
        Substitute.For<ISupportAccessAuditEventRepository>();

    private GetSupportAccessAuditEventsQueryHandler CreateHandler() =>
        new(_sessionRepository, _auditEventRepository);

    /// <summary>
    /// The disclosure case: a caller presents their own tenant id alongside a session id belonging to a
    /// different tenant. Authorization sees a request scoped to the caller's own tenant and allows it, so
    /// only the ownership check stands between the caller and another customer's support history.
    /// </summary>
    [Test]
    public async Task AuditEvents_ForASessionOwnedByAnotherTenant_DiscloseNothingAndAreNeverRead()
    {
        var callerTenantId = Guid.CreateVersion7();
        var otherTenantId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();

        _sessionRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(CreateSession(otherTenantId));

        var result = await CreateHandler().Handle(
            new GetSupportAccessAuditEventsQuery { TargetTenantId = callerTenantId, SessionId = sessionId },
            CancellationToken.None);

        await Assert.That(result.Items).IsEmpty();
        await Assert.That(result.TotalCount).IsEqualTo(0);

        // The audit rows must never even be fetched. Reading then discarding them would leave the real
        // contents one refactor away from being returned.
        await _auditEventRepository.DidNotReceive().ListForSessionAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A session id that does not resolve must read as "nothing here", not as an error that confirms the
    /// id was well-formed or that some session exists.
    /// </summary>
    [Test]
    public async Task AuditEvents_ForAnUnknownSession_DiscloseNothing()
    {
        _sessionRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SupportAccessSession?)null);

        var result = await CreateHandler().Handle(
            new GetSupportAccessAuditEventsQuery
            {
                TargetTenantId = Guid.CreateVersion7(),
                SessionId = Guid.CreateVersion7()
            },
            CancellationToken.None);

        await Assert.That(result.Items).IsEmpty();
        await Assert.That(result.TotalCount).IsEqualTo(0);
        await _auditEventRepository.DidNotReceive().ListForSessionAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>The matching-tenant case must still work, or the test above would pass trivially.</summary>
    [Test]
    public async Task AuditEvents_ForTheOwningTenant_AreReturned()
    {
        var tenantId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();

        var session = CreateSession(tenantId);
        _sessionRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(session);
        _auditEventRepository.ListForSessionAsync(sessionId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([CreateAuditEvent(session)]);

        var result = await CreateHandler().Handle(
            new GetSupportAccessAuditEventsQuery { TargetTenantId = tenantId, SessionId = sessionId },
            CancellationToken.None);

        await Assert.That(result.Items).HasSingleItem();
        await Assert.That(result.TotalCount).IsEqualTo(1);
    }

    private static SupportAccessSession CreateSession(Guid targetTenantId) =>
        SupportAccessSession.Start(
            actorUserId: Guid.CreateVersion7(),
            targetTenantId: targetTenantId,
            mode: SupportAccessModeEnum.ReadOnly,
            reasonCode: "investigation",
            reasonText: "disclosure boundary fixture",
            ticketReference: "TICKET-1",
            startedAtUtc: DateTimeOffset.UtcNow,
            expiresAtUtc: DateTimeOffset.UtcNow.AddHours(1));

    private static SupportAccessAuditEvent CreateAuditEvent(SupportAccessSession session) =>
        SupportAccessAuditEvent.CreateLifecycleEvent(
            session,
            SupportAccessAuditEventTypeEnum.Started,
            outcome: "succeeded",
            occurredAtUtc: DateTimeOffset.UtcNow);
}
