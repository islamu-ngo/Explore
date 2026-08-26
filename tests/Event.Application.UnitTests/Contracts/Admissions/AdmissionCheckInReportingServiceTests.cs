// ABOUTME: Specifies authorized exact-target summaries and redacted cursor-based admission audit pages.
// ABOUTME: Proves tenant/event isolation, bounded paging, cancellation, safe failures, and output minimization.

using System.Reflection;
using Explore.Application.Contracts.Admissions;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Exceptions;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace ApplicationUnitTests.Contracts.Admissions;

public sealed class AdmissionCheckInReportingServiceTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid EventId = Guid.CreateVersion7();
    private static readonly Guid TargetId = Guid.CreateVersion7();
    private static readonly Guid ActorId = Guid.CreateVersion7();
    private static readonly DateTime OccurredAt = new(2026, 8, 26, 12, 34, 56, DateTimeKind.Utc);

    [Test]
    public async Task DetailReturnsExactFactAndUndoOnlyWhileThatFactIsActive()
    {
        AdmissionCheckInEvent fact = Event(AdmissionCheckInActionEnum.CheckIn, staff: true);
        AdmissionCheckInState state = AdmissionCheckInState.Rehydrate(
            Guid.CreateVersion7(),
            TenantId,
            fact.AdmissionTicketId,
            TargetId,
            fact.Id,
            1,
            1,
            Guid.CreateVersion7());
        IAdmissionCheckInReportingRepository repository =
            Substitute.For<IAdmissionCheckInReportingRepository>();
        repository.GetEventAsync(TenantId, EventId, fact.Id, Arg.Any<CancellationToken>())
            .Returns(fact);
        repository.GetStateAsync(
                TenantId,
                fact.AdmissionTicketId,
                TargetId,
                Arg.Any<CancellationToken>())
            .Returns(state);
        var service = new AdmissionCheckInReportingService(
            AllowedAuthorization(),
            Substitute.For<IAdmissionCheckInSummaryQuery>(),
            repository);

        AdmissionCheckInDetail? detail = await service.GetDetailAsync(
            new AdmissionCheckInDetailRequest(TenantId, EventId, fact.Id, ActorId),
            CancellationToken.None);

        await Assert.That(detail).IsNotNull();
        await Assert.That(detail!.Result.CheckInId).IsEqualTo(fact.Id);
        await Assert.That(detail.Result.TargetId).IsEqualTo(TargetId);
        await Assert.That(detail.Result.Outcome).IsEqualTo(AdmissionCheckInOutcome.CheckedIn);
        await Assert.That(detail.CanUndo).IsTrue();
    }

    [Test]
    public async Task AuthorizedSummaryCountsOnlyExactTargetStableResultsAndState()
    {
        IAuthorizationProvider authorization = AllowedAuthorization();
        IAdmissionCheckInSummaryQuery summaryQuery = Substitute.For<IAdmissionCheckInSummaryQuery>();
        summaryQuery.GetAsync(TenantId, EventId, TargetId, Arg.Any<CancellationToken>())
            .Returns(new AdmissionCheckInSummaryProjection(
                TenantId,
                EventId,
                TargetId,
                AdmissionTargetTypeEnum.Event,
                1,
                1,
                1,
                1,
                OccurredAt));
        var service = new AdmissionCheckInReportingService(
            authorization,
            summaryQuery,
            Substitute.For<IAdmissionCheckInReportingRepository>());

        AdmissionCheckInSummary? result = await service.GetSummaryAsync(
            new AdmissionCheckInSummaryRequest(TenantId, EventId, TargetId, ActorId),
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TargetType).IsEqualTo(AdmissionTargetTypeEnum.Event);
        await Assert.That(result.ResultCounts).IsEquivalentTo([
            new AdmissionCheckInResultCount(AdmissionCheckInOutcome.CheckedIn, 1),
            new AdmissionCheckInResultCount(AdmissionCheckInOutcome.Undone, 1)]);
        await Assert.That(result.StateCounts).IsEquivalentTo([
            new AdmissionCheckInStateCount(AdmissionCheckInSummaryState.Active, 1),
            new AdmissionCheckInStateCount(AdmissionCheckInSummaryState.Inactive, 1)]);
        await Assert.That(result.LastActivityTimeBucketUtc)
            .IsEqualTo(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));
        AuthorizationRequest authorizationRequest = (AuthorizationRequest)authorization.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IAuthorizationProvider.AuthorizeAsync))
            .GetArguments()[0]!;
        await Assert.That(authorizationRequest.ResourceId).IsEqualTo(EventId.ToString("D"));
        await Assert.That(authorizationRequest.Scope!.TenantId).IsEqualTo(TenantId.ToString("D"));
        await Assert.That(authorizationRequest.Subject!.UserId).IsEqualTo(ActorId);
        await summaryQuery.Received(1).GetAsync(
            TenantId, EventId, TargetId, Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>());
            summaryQuery.GetAsync(TenantId, EventId, TargetId, Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task SummaryUsesOneProjectionCallIndependentOfAggregateRowCountAndHasNoEntityPageLoop()
    {
        IAdmissionCheckInSummaryQuery summaryQuery = Substitute.For<IAdmissionCheckInSummaryQuery>();
        summaryQuery.GetAsync(TenantId, EventId, TargetId, Arg.Any<CancellationToken>())
            .Returns(new AdmissionCheckInSummaryProjection(
                TenantId,
                EventId,
                TargetId,
                AdmissionTargetTypeEnum.Event,
                9_000_000,
                8_000_000,
                7_000_000,
                6_000_000,
                OccurredAt));
        IAdmissionCheckInReportingRepository repository = Substitute.For<IAdmissionCheckInReportingRepository>();
        var service = new AdmissionCheckInReportingService(AllowedAuthorization(), summaryQuery, repository);

        AdmissionCheckInSummary? result = await service.GetSummaryAsync(
            new AdmissionCheckInSummaryRequest(TenantId, EventId, TargetId, ActorId),
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await summaryQuery.Received(1).GetAsync(
            TenantId, EventId, TargetId, Arg.Any<CancellationToken>());
        await Assert.That(typeof(IAdmissionCheckInReportingRepository).GetMethods().Any(method =>
            method.Name.Contains("TargetEventsPage", StringComparison.Ordinal) ||
            method.Name.Contains("TargetStatesPage", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task GenericAbsencePreventsDeniedOrWrongLineageEnumeration()
    {
        IAuthorizationProvider denied = Substitute.For<IAuthorizationProvider>();
        denied.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Deny(AuthorizationProviderMetadata.Local));
        IAdmissionCheckInSummaryQuery summaryQuery = Substitute.For<IAdmissionCheckInSummaryQuery>();
        IAdmissionCheckInReportingRepository repository = Substitute.For<IAdmissionCheckInReportingRepository>();
        var service = new AdmissionCheckInReportingService(denied, summaryQuery, repository);

        AdmissionCheckInSummary? deniedResult = await service.GetSummaryAsync(
            new AdmissionCheckInSummaryRequest(TenantId, EventId, TargetId, ActorId),
            CancellationToken.None);

        await Assert.That(deniedResult).IsNull();
        await summaryQuery.DidNotReceiveWithAnyArgs().GetAsync(default, default, default, default);

        IAuthorizationProvider allowed = AllowedAuthorization();
        summaryQuery.GetAsync(TenantId, EventId, TargetId, Arg.Any<CancellationToken>())
            .Returns((AdmissionCheckInSummaryProjection?)null);
        service = new AdmissionCheckInReportingService(allowed, summaryQuery, repository);
        AdmissionCheckInSummary? missingResult = await service.GetSummaryAsync(
            new AdmissionCheckInSummaryRequest(TenantId, EventId, TargetId, ActorId),
            CancellationToken.None);
        await Assert.That(missingResult).IsNull();
    }

    [Test]
    public async Task AuditPageUsesOpaqueKeysetCursorBoundedSizeAndExportsOnlyStableRedactedFacts()
    {
        IAuthorizationProvider authorization = AllowedAuthorization();
        IAdmissionCheckInReportingRepository repository = Substitute.For<IAdmissionCheckInReportingRepository>();
        AdmissionCheckInEvent eventRow = Event(AdmissionCheckInActionEnum.Undo, true);
        AdmissionCheckInEvent secondRow = Event(AdmissionCheckInActionEnum.CheckIn, false);
        repository.ListEventAuditPageAsync(TenantId, EventId, null, 3, Arg.Any<CancellationToken>())
            .Returns([eventRow, secondRow, Event(AdmissionCheckInActionEnum.Undo, true)]);
        repository.ListTargetsAsync(TenantId, EventId, Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([Target()]);
        var service = new AdmissionCheckInReportingService(
            authorization, Substitute.For<IAdmissionCheckInSummaryQuery>(), repository);

        AdmissionCheckInAuditPage? result = await service.GetAuditPageAsync(
            new AdmissionCheckInAuditPageRequest(TenantId, EventId, ActorId, Cursor: null, PageSize: 2),
            CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(typeof(AdmissionCheckInAuditPageRequest).GetProperty("Cursor")!.PropertyType)
            .IsEqualTo(typeof(string));
        await Assert.That(result!.Items.Count).IsEqualTo(2);
        await Assert.That(result.NextCursor).IsEqualTo(result.Items[1].Cursor);
        await Assert.That(AdmissionCheckInAuditCursor.TryDecode(
            result.NextCursor, out AdmissionCheckInAuditCursor? boundary)).IsTrue();
        await Assert.That(boundary).IsEqualTo(
            new AdmissionCheckInAuditCursor(secondRow.OccurredAtUtc, secondRow.Id));
        await repository.Received(1).ListEventAuditPageAsync(
            TenantId, EventId, null, 3, Arg.Any<CancellationToken>());
        AdmissionCheckInAuditItem item = result.Items[0];
        await Assert.That(item.Action).IsEqualTo(AdmissionCheckInAction.Undo);
        await Assert.That(item.Outcome).IsEqualTo(AdmissionCheckInOutcome.Undone);
        await Assert.That(item.TargetType).IsEqualTo(AdmissionTargetTypeEnum.Event);
        await Assert.That(item.OccurredAtTimeBucketUtc)
            .IsEqualTo(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));

        string[] publicNames = typeof(AdmissionCheckInAuditItem).GetProperties()
            .Select(property => property.Name).ToArray();
        string[] forbidden = ["Ticket", "Participant", "Order", "Credential", "CapabilityId",
            "Digest", "Device", "ActorId", "Reason", "TargetId", "TenantId", "EventId"];
        await Assert.That(publicNames.Any(name => forbidden.Any(term =>
            name.Contains(term, StringComparison.OrdinalIgnoreCase)))).IsFalse();
    }

    [Test]
    public async Task InvalidCursorOrPageSizeFailsBeforeAuthorizationAndRepository()
    {
        IAuthorizationProvider authorization = Substitute.For<IAuthorizationProvider>();
        IAdmissionCheckInReportingRepository repository = Substitute.For<IAdmissionCheckInReportingRepository>();
        var service = new AdmissionCheckInReportingService(
            authorization, Substitute.For<IAdmissionCheckInSummaryQuery>(), repository);

        AdmissionCheckInAuditPage? malformed = await service.GetAuditPageAsync(
            new AdmissionCheckInAuditPageRequest(TenantId, EventId, ActorId, Cursor: "not-a-cursor", PageSize: 10),
            CancellationToken.None);
        AdmissionCheckInAuditPage? excessive = await service.GetAuditPageAsync(
            new AdmissionCheckInAuditPageRequest(TenantId, EventId, ActorId, Cursor: null, PageSize: 101),
            CancellationToken.None);

        await Assert.That(malformed).IsNull();
        await Assert.That(excessive).IsNull();
        await authorization.DidNotReceiveWithAnyArgs().AuthorizeAsync(default!, default);
        await repository.DidNotReceiveWithAnyArgs().ListEventAuditPageAsync(
            default, default, default, default, default);
    }

    [Test]
    public async Task CancellationPropagatesAndDependencyFailureBecomesGenericUnavailable()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        IAuthorizationProvider authorization = Substitute.For<IAuthorizationProvider>();
        authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), cancellation.Token)
            .Returns(Task.FromException<AuthorizationDecision>(
                new OperationCanceledException(cancellation.Token)));
        var service = new AdmissionCheckInReportingService(
            authorization,
            Substitute.For<IAdmissionCheckInSummaryQuery>(),
            Substitute.For<IAdmissionCheckInReportingRepository>());

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.GetSummaryAsync(
            new AdmissionCheckInSummaryRequest(TenantId, EventId, TargetId, ActorId),
            cancellation.Token));

        authorization = AllowedAuthorization();
        IAdmissionCheckInSummaryQuery summaryQuery = Substitute.For<IAdmissionCheckInSummaryQuery>();
        summaryQuery.GetAsync(TenantId, EventId, TargetId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AdmissionCheckInSummaryProjection?>(
                new InvalidOperationException("db detail")));
        service = new AdmissionCheckInReportingService(
            authorization, summaryQuery, Substitute.For<IAdmissionCheckInReportingRepository>());
        AdmissionCheckInUnavailableException? exception =
            await Assert.ThrowsAsync<AdmissionCheckInUnavailableException>(() => service.GetSummaryAsync(
                new AdmissionCheckInSummaryRequest(TenantId, EventId, TargetId, ActorId),
                CancellationToken.None));
        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).DoesNotContain("db detail");
    }

    private static IAuthorizationProvider AllowedAuthorization()
    {
        IAuthorizationProvider authorization = Substitute.For<IAuthorizationProvider>();
        authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local));
        return authorization;
    }

    private static AdmissionTarget Target() => AdmissionTarget.Create(
        TargetId, TenantId, EventId, AdmissionTargetTypeEnum.Event, null, null);

    private static AdmissionCheckInEvent Event(AdmissionCheckInActionEnum action, bool staff)
    {
        ConstructorInfo constructor = typeof(AdmissionCheckInEvent).GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(value => value.GetParameters().Length == 11);
        return (AdmissionCheckInEvent)constructor.Invoke([
            Guid.CreateVersion7(), TenantId, Guid.CreateVersion7(), TargetId, 1L, action,
            staff ? Guid.CreateVersion7() : null, staff ? null : Guid.CreateVersion7(),
            action == AdmissionCheckInActionEnum.Undo
                ? AdmissionCheckInUndoReasonCodeEnum.OperatorCorrection
                : null,
            OccurredAt,
            action == AdmissionCheckInActionEnum.Undo ? Guid.CreateVersion7() : null]);
    }
}
