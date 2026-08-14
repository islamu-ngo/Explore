// ABOUTME: Pins fail-closed batched EventLocation management authorization and audit-before-return behavior.
// ABOUTME: Verifies exact event policy inputs, output decisions, and PII-free audit facts.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;

namespace Event.Application.UnitTests.Services;

public sealed class EventLocationManagementAuthorizationServiceTests
{
    [Test]
    [Category("EventLocationPrivacy")]
    [Category("Todo10EventLocationManagementAuthorization")]
    [Category("Todo10EventLocationManagementAuthorizationPin")]
    public async Task AuthorizeManyAsync_TwoPlacementsOnOneEvent_UsesOneExactPolicyCheckAndAuditsBothDecisions()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid requesterUserId = Guid.CreateVersion7();
        Guid correlationId = Guid.CreateVersion7();
        EventLocation[] placements =
        [
            CreatePlacement(tenantId, eventId),
            CreatePlacement(tenantId, eventId)
        ];
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetAuthorizationTargetsByIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { eventId })),
                Arg.Any<CancellationToken>())
            .Returns([CreateEvent(tenantId, eventId)]);
        var authorizationProvider = Substitute.For<IAuthorizationProvider>();
        IReadOnlyList<AuthorizationRequest>? observedChecks = null;
        authorizationProvider.IsAllowedBatchAsync(
                Arg.Do<IReadOnlyList<AuthorizationRequest>>(checks => observedChecks = checks),
                Arg.Any<CancellationToken>())
            .Returns([true]);
        var currentUser = AuthenticatedUser(requesterUserId);
        var auditService = Substitute.For<IEventLocationExactReadAuditService>();
        IReadOnlyCollection<EventLocationExactReadAuditRequest>? observedAudits = null;
        auditService.RecordManyAsync(
                Arg.Do<IReadOnlyCollection<EventLocationExactReadAuditRequest>>(audits => observedAudits = audits),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var service = CreateService(eventRepository, authorizationProvider, currentUser, auditService);

        IReadOnlyDictionary<Guid, bool> decisions = await service.AuthorizeManyAsync(
            placements,
            EventLocationExactReadPurposeEnum.EventManagement,
            correlationId,
            traceId: null,
            CancellationToken.None);

        await Assert.That(decisions.Count).IsEqualTo(2);
        await Assert.That(decisions.Values).IsEquivalentTo([true, true]);
        await Assert.That(observedChecks).IsNotNull();
        await Assert.That(observedChecks!).HasSingleItem();
        AuthorizationRequest check = observedChecks.Single();
        await Assert.That(check.ResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(check.ResourceId).IsEqualTo(eventId.ToString());
        await Assert.That(check.Action).IsEqualTo(AuthorizationActions.Events.ViewManagement);
        await Assert.That(check.Scope?.TenantId).IsEqualTo(tenantId.ToString());
        await Assert.That(check.ResourceAttributes?["tenantId"]).IsEqualTo(tenantId.ToString());
        await Assert.That(observedAudits).IsNotNull();
        await Assert.That(observedAudits!.Select(audit => audit.EventLocationId))
            .IsEquivalentTo(placements.Select(placement => placement.Id));
        await Assert.That(observedAudits.All(audit =>
            audit.TenantId == tenantId
            && audit.RequesterUserId == requesterUserId
            && audit.Purpose == EventLocationExactReadPurposeEnum.EventManagement
            && audit.WasAuthorized
            && audit.CorrelationId == correlationId
            && audit.TraceId is null)).IsTrue();
        await eventRepository.Received(1).GetAuthorizationTargetsByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
        await authorizationProvider.Received(1).IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationRequest>>(),
            Arg.Any<CancellationToken>());
        await auditService.Received(1).RecordManyAsync(
            Arg.Any<IReadOnlyCollection<EventLocationExactReadAuditRequest>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Category("Todo10EventLocationManagementAuthorization")]
    [Category("Todo10EventLocationManagementAuthorizationPin")]
    public async Task AuthorizeManyAsync_AuditFailure_ReturnsNoDecision()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        EventLocation placement = CreatePlacement(tenantId, eventId);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetAuthorizationTargetsByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([CreateEvent(tenantId, eventId)]);
        var authorizationProvider = Substitute.For<IAuthorizationProvider>();
        authorizationProvider.IsAllowedBatchAsync(
                Arg.Any<IReadOnlyList<AuthorizationRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns([true]);
        var auditService = Substitute.For<IEventLocationExactReadAuditService>();
        auditService.RecordManyAsync(
                Arg.Any<IReadOnlyCollection<EventLocationExactReadAuditRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("forced audit failure"));
        var service = CreateService(
            eventRepository,
            authorizationProvider,
            AuthenticatedUser(Guid.CreateVersion7()),
            auditService);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AuthorizeManyAsync(
                [placement],
                EventLocationExactReadPurposeEnum.EventManagement,
                Guid.CreateVersion7(),
                null,
                CancellationToken.None));

        await Assert.That(exception.Message).IsEqualTo("forced audit failure");
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Category("Todo10EventLocationManagementAuthorization")]
    [Category("Todo10EventLocationManagementAuthorizationRed")]
    public async Task AuthorizeManyAsync_MissingTarget_DeniesThroughOneProviderBatchAndAuditsDenial()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid requesterUserId = Guid.CreateVersion7();
        EventLocation placement = CreatePlacement(tenantId, eventId);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetAuthorizationTargetsByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        var authorizationProvider = Substitute.For<IAuthorizationProvider>();
        authorizationProvider.IsAllowedBatchAsync(
                Arg.Any<IReadOnlyList<AuthorizationRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        var auditService = Substitute.For<IEventLocationExactReadAuditService>();
        IReadOnlyCollection<EventLocationExactReadAuditRequest>? observedAudits = null;
        auditService.RecordManyAsync(
                Arg.Do<IReadOnlyCollection<EventLocationExactReadAuditRequest>>(audits => observedAudits = audits),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var service = CreateService(
            eventRepository,
            authorizationProvider,
            AuthenticatedUser(requesterUserId),
            auditService);

        IReadOnlyDictionary<Guid, bool> decisions = await service.AuthorizeManyAsync(
            [placement],
            EventLocationExactReadPurposeEnum.EventManagement,
            Guid.CreateVersion7(),
            null,
            CancellationToken.None);

        await Assert.That(decisions[placement.Id]).IsFalse();
        await Assert.That(observedAudits).IsNotNull();
        await Assert.That(observedAudits!).HasSingleItem();
        await Assert.That(observedAudits.Single().WasAuthorized).IsFalse();
        ICall providerCall = authorizationProvider.ReceivedCalls().Single();
        var observedChecks = (IReadOnlyList<AuthorizationRequest>)providerCall.GetArguments()[0]!;
        await Assert.That(observedChecks).IsEmpty();
        await authorizationProvider.Received(1).IsAllowedBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationRequest>>(),
            Arg.Any<CancellationToken>());
    }

    private static EventLocationManagementAuthorizationService CreateService(
        IEventRepository eventRepository,
        IAuthorizationProvider authorizationProvider,
        ICurrentUserService currentUser,
        IEventLocationExactReadAuditService auditService) => new(
            eventRepository,
            authorizationProvider,
            currentUser,
            auditService,
            Substitute.For<ILogger<EventLocationManagementAuthorizationService>>());

    private static ICurrentUserService AuthenticatedUser(Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        return currentUser;
    }

    private static EventLocation CreatePlacement(Guid tenantId, Guid eventId) =>
        EventLocation.CreateToBeAnnounced(
            tenantId,
            eventId,
            Guid.CreateVersion7(),
            new DateTime(2026, 7, 19, 12, 0, 0, DateTimeKind.Utc));

    private static Explore.Domain.Event CreateEvent(Guid tenantId, Guid eventId) => new()
    {
        Id = eventId,
        TenantId = tenantId,
        Tenant = null!,
        ActorId = Guid.CreateVersion7(),
        Actor = null!,
        Title = "Authorization target",
        EventStatus = null!,
        EventFormat = null!,
        VisibilityType = null!
    };
}
