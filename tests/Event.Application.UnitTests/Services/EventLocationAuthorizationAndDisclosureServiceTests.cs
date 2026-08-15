// ABOUTME: Executable security and batching contract for EventLocation management authorization and disclosure.
// ABOUTME: Proves fail-closed audited decisions, normalized purpose scope, bounded I/O, and immutable results.

using System.Collections.Immutable;
using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Exceptions;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

[Category("EventLocationPrivacy")]
public sealed class EventLocationAuthorizationAndDisclosureServiceTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid RequesterUserId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 14, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task ManagementAuthorization_BatchesDistinctEventsAndPersistsEveryDecisionBeforeReturn()
    {
        Guid[] orderedEventIds = Enumerable.Range(0, 3)
            .Select(_ => Guid.CreateVersion7())
            .Order()
            .ToArray();
        Guid firstEventId = orderedEventIds[0];
        Guid secondEventId = orderedEventIds[1];
        Guid missingEventId = orderedEventIds[2];
        EventLocation[] candidates =
        [
            CreatePlacement(firstEventId),
            CreatePlacement(firstEventId),
            CreatePlacement(secondEventId),
            CreatePlacement(missingEventId)
        ];
        Explore.Domain.Event[] targets =
        [
            CreateEvent(firstEventId),
            CreateEvent(secondEventId)
        ];
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetAuthorizationTargetsByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(targets);
        var authorizationProvider = Substitute.For<IAuthorizationProvider>();
        authorizationProvider.AuthorizeBatchAsync(
                Arg.Any<IReadOnlyList<AuthorizationRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns([
                AuthorizationDecision.Allow(AuthorizationProviderMetadata.Runtime),
                AuthorizationDecision.Deny(AuthorizationProviderMetadata.Runtime)
            ]);
        var auditService = Substitute.For<IEventLocationExactReadAuditService>();
        EventLocationExactReadAuditRequest[] capturedAudits = [];
        var auditEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var auditRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        auditService.RecordManyAsync(
                Arg.Any<IReadOnlyCollection<EventLocationExactReadAuditRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedAudits = call.Arg<IReadOnlyCollection<EventLocationExactReadAuditRequest>>().ToArray();
                auditEntered.TrySetResult();
                return auditRelease.Task;
            });
        using var cancellation = new CancellationTokenSource();
        var service = CreateManagementService(
            eventRepository,
            authorizationProvider,
            auditService,
            CreateCurrentUser(RequesterUserId));

        Task<IReadOnlyDictionary<Guid, bool>> pending = service.AuthorizeManyAsync(
            candidates,
            EventLocationExactReadPurposeEnum.EventManagement,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            cancellation.Token);
        await auditEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.That(pending.IsCompleted).IsFalse();
        await Assert.That(capturedAudits.Length).IsEqualTo(candidates.Length);
        await Assert.That(capturedAudits.Count(request => request.WasAuthorized)).IsEqualTo(2);
        await Assert.That(capturedAudits.All(request =>
            request.TenantId == TenantId
            && request.RequesterUserId == RequesterUserId
            && request.Purpose == EventLocationExactReadPurposeEnum.EventManagement)).IsTrue();

        auditRelease.TrySetResult();
        IReadOnlyDictionary<Guid, bool> decisions = await pending;

        await Assert.That(decisions.Count).IsEqualTo(candidates.Length);
        await Assert.That(decisions[candidates[0].Id]).IsTrue();
        await Assert.That(decisions[candidates[1].Id]).IsTrue();
        await Assert.That(decisions[candidates[2].Id]).IsFalse();
        await Assert.That(decisions[candidates[3].Id]).IsFalse();
        await Assert.That(decisions.GetType().Namespace).IsEqualTo("System.Collections.Immutable");
        await eventRepository.Received(1).GetAuthorizationTargetsByIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 3),
            cancellation.Token);
        await authorizationProvider.Received(1).AuthorizeBatchAsync(
            Arg.Is<IReadOnlyList<AuthorizationRequest>>(checks =>
                checks.Count == 2
                && checks.All(check =>
                    check.ResourceKind == ResourceKinds.Event
                    && check.Action == AuthorizationActions.Events.ViewManagement)),
            cancellation.Token);
        await auditService.Received(1).RecordManyAsync(
            Arg.Any<IReadOnlyCollection<EventLocationExactReadAuditRequest>>(),
            cancellation.Token);
    }

    [Test]
    public async Task ManagementAuthorization_ProviderFailureIsAuditedAsDenyAndAuditFailurePreventsReturn()
    {
        Guid eventId = Guid.CreateVersion7();
        EventLocation candidate = CreatePlacement(eventId);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetAuthorizationTargetsByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([CreateEvent(eventId)]);
        var authorizationProvider = Substitute.For<IAuthorizationProvider>();
        authorizationProvider.AuthorizeBatchAsync(
                Arg.Any<IReadOnlyList<AuthorizationRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<AuthorizationDecision>>>(_ => throw new InvalidOperationException("provider unavailable"));
        var auditService = Substitute.For<IEventLocationExactReadAuditService>();
        EventLocationExactReadAuditRequest[] captured = [];
        auditService.RecordManyAsync(
                Arg.Any<IReadOnlyCollection<EventLocationExactReadAuditRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.Arg<IReadOnlyCollection<EventLocationExactReadAuditRequest>>().ToArray();
                return Task.CompletedTask;
            });
        var service = CreateManagementService(
            eventRepository,
            authorizationProvider,
            auditService,
            CreateCurrentUser(RequesterUserId));

        IReadOnlyDictionary<Guid, bool> denied = await service.AuthorizeManyAsync(
            [candidate],
            EventLocationExactReadPurposeEnum.EventManagement,
            null,
            null,
            CancellationToken.None);

        await Assert.That(denied[candidate.Id]).IsFalse();
        await Assert.That(captured.Single().WasAuthorized).IsFalse();

        auditService.RecordManyAsync(
                Arg.Any<IReadOnlyCollection<EventLocationExactReadAuditRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("audit unavailable"));

        await Assert.That(async () => await service.AuthorizeManyAsync(
                [candidate],
                EventLocationExactReadPurposeEnum.EventManagement,
                null,
                null,
                CancellationToken.None))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ExactReadAudit_UsesServerTimeAndCarriesOnlyTypedPiiFreeEvidence()
    {
        var repository = Substitute.For<IEventLocationExactReadAuditRepository>();
        EventLocationExactReadAudit[] captured = [];
        using var cancellation = new CancellationTokenSource();
        repository.AppendManyAsync(
                Arg.Any<IReadOnlyCollection<EventLocationExactReadAudit>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.Arg<IReadOnlyCollection<EventLocationExactReadAudit>>().ToArray();
                return Task.CompletedTask;
            });
        var service = new EventLocationExactReadAuditService(repository, new FixedTimeProvider(Now));

        await service.RecordManyAsync(
        [
            new(TenantId, Guid.CreateVersion7(), RequesterUserId,
                EventLocationExactReadPurposeEnum.EventManagement, true),
            new(TenantId, Guid.CreateVersion7(), RequesterUserId,
                EventLocationExactReadPurposeEnum.PrivacyRemediation, false)
        ], cancellation.Token);

        await Assert.That(captured.Length).IsEqualTo(2);
        await Assert.That(captured.All(audit => audit.OccurredAtUtc == Now.UtcDateTime)).IsTrue();
        await Assert.That(captured.All(audit =>
            audit.CorrelationId.HasValue ^ audit.TraceId.HasValue)).IsTrue();
        await Assert.That(captured
            .Select(audit => (audit.CorrelationId, audit.TraceId))
            .Distinct()
            .Count()).IsEqualTo(1);
        await repository.Received(1).AppendManyAsync(
            Arg.Any<IReadOnlyCollection<EventLocationExactReadAudit>>(),
            cancellation.Token);

        string[] forbiddenNames =
        [
            "LocationId", "Address", "Postcode", "Latitude", "Longitude", "RoomName",
            "RoomDescription", "VenueName", "Description", "Value", "Payload"
        ];
        string[] requestProperties = typeof(EventLocationExactReadAuditRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();
        string[] auditProperties = typeof(EventLocationExactReadAudit)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();
        foreach (string forbidden in forbiddenNames)
        {
            await Assert.That(requestProperties).DoesNotContain(forbidden);
            await Assert.That(auditProperties).DoesNotContain(forbidden);
        }
    }

    [Test]
    public async Task AuthorizationAndDisclosure_ForwardCancellationWithoutReturningDecisions()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        Guid eventId = Guid.CreateVersion7();
        EventLocation candidate = CreatePlacement(eventId);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetAuthorizationTargetsByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                cancellation.Token)
            .Returns(Task.FromCanceled<IReadOnlyList<Explore.Domain.Event>>(cancellation.Token));
        var authorizationProvider = Substitute.For<IAuthorizationProvider>();
        var auditService = Substitute.For<IEventLocationExactReadAuditService>();
        var management = CreateManagementService(
            eventRepository,
            authorizationProvider,
            auditService,
            CreateCurrentUser(RequesterUserId));

        await Assert.That(async () => await management.AuthorizeManyAsync(
                [candidate],
                EventLocationExactReadPurposeEnum.EventManagement,
                null,
                null,
                cancellation.Token))
            .Throws<OperationCanceledException>();
        await authorizationProvider.DidNotReceive().AuthorizeBatchAsync(
            Arg.Any<IReadOnlyList<AuthorizationRequest>>(),
            Arg.Any<CancellationToken>());
        await auditService.DidNotReceive().RecordManyAsync(
            Arg.Any<IReadOnlyCollection<EventLocationExactReadAuditRequest>>(),
            Arg.Any<CancellationToken>());

        var locationRepository = Substitute.For<IEventLocationRepository>();
        locationRepository.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                cancellation.Token)
            .Returns(Task.FromCanceled<IReadOnlyList<EventLocation>>(cancellation.Token));
        var governance = Substitute.For<ILocationPrivacyGovernanceService>();
        var disclosure = CreateDisclosureService(
            locationRepository,
            Substitute.For<ILocationRoomRepository>(),
            Substitute.For<IEventRegistrationRepository>(),
            Substitute.For<IEventLocationRegistrationAccessService>(),
            governance,
            Substitute.For<IEventLocationManagementAuthorizationService>(),
            CreateCurrentUser(RequesterUserId));

        await Assert.That(async () => await disclosure.ResolveManyAsync(
        [
            new(TenantId, eventId, candidate.Id, null, null, EventLocationDisclosurePurpose.Management)
        ], cancellation.Token)).Throws<OperationCanceledException>();
        await governance.DidNotReceive().ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Disclosure_PublicBatchStripsIdentitySpansEventsAndHidesConflictingRoomContext()
    {
        Guid firstEventId = Guid.CreateVersion7();
        Guid secondEventId = Guid.CreateVersion7();
        EventLocation first = CreateTba(firstEventId);
        EventLocation second = CreateTba(secondEventId);
        var locationRepository = Substitute.For<IEventLocationRepository>();
        locationRepository.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([first, second]);
        var roomRepository = Substitute.For<ILocationRoomRepository>();
        var registrationRepository = Substitute.For<IEventRegistrationRepository>();
        var registrationAccess = Substitute.For<IEventLocationRegistrationAccessService>();
        var governance = Substitute.For<ILocationPrivacyGovernanceService>();
        governance.ResolveAsync(TenantId, Arg.Any<CancellationToken>()).Returns(ResolvedGovernance());
        var management = Substitute.For<IEventLocationManagementAuthorizationService>();
        var unauthenticated = CreateCurrentUser(null);
        var service = CreateDisclosureService(
            locationRepository,
            roomRepository,
            registrationRepository,
            registrationAccess,
            governance,
            management,
            unauthenticated);

        IReadOnlyDictionary<Guid, EventLocationDisclosureResult> result = await service.ResolveManyAsync(
        [
            new(TenantId, firstEventId, first.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), EventLocationDisclosurePurpose.Public),
            new(TenantId, firstEventId, first.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), EventLocationDisclosurePurpose.Public),
            new(TenantId, secondEventId, second.Id, null, Guid.CreateVersion7(), EventLocationDisclosurePurpose.Public)
        ], CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.GetType().Namespace).IsEqualTo("System.Collections.Immutable");
        await Assert.That(result.Values.All(item =>
            item.Purpose == EventLocationDisclosurePurpose.Public
            && item.State == EventLocationDisclosureState.ToBeAnnounced
            && item.LocationId is null
            && item.Values is null)).IsTrue();
        await locationRepository.Received(1).GetByIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
            Arg.Any<CancellationToken>());
        await roomRepository.DidNotReceive().GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
        await governance.Received(1).ResolveAsync(TenantId, Arg.Any<CancellationToken>());
        await registrationRepository.DidNotReceive().GetLocationAccessCoverageAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await management.DidNotReceive().AuthorizeManyAsync(
            Arg.Any<IReadOnlyCollection<EventLocation>>(),
            Arg.Any<EventLocationExactReadPurposeEnum>(),
            Arg.Any<Guid?>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Disclosure_ManagementPerformsOneOrderedIoBatchAndNeverReadsRegistrationCoverage()
    {
        Guid eventId = Guid.CreateVersion7();
        EventLocation[] placements = [CreatePlacement(eventId), CreatePlacement(eventId)];
        Guid[] roomIds = [Guid.CreateVersion7(), Guid.CreateVersion7()];
        var calls = new List<string>();
        var locationRepository = Substitute.For<IEventLocationRepository>();
        locationRepository.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("event-locations");
                return placements;
            });
        var roomRepository = Substitute.For<ILocationRoomRepository>();
        roomRepository.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("rooms");
                return Array.Empty<LocationRoom>();
            });
        var registrationRepository = Substitute.For<IEventRegistrationRepository>();
        var registrationAccess = Substitute.For<IEventLocationRegistrationAccessService>();
        var governance = Substitute.For<ILocationPrivacyGovernanceService>();
        governance.ResolveAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("governance");
                return ResolvedGovernance();
            });
        var management = Substitute.For<IEventLocationManagementAuthorizationService>();
        management.AuthorizeManyAsync(
                Arg.Any<IReadOnlyCollection<EventLocation>>(),
                Arg.Any<EventLocationExactReadPurposeEnum>(),
                Arg.Any<Guid?>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("management-authorization");
                return placements.ToImmutableDictionary(item => item.Id, _ => true);
            });
        var service = CreateDisclosureService(
            locationRepository,
            roomRepository,
            registrationRepository,
            registrationAccess,
            governance,
            management,
            CreateCurrentUser(RequesterUserId));

        IReadOnlyDictionary<Guid, EventLocationDisclosureResult> result = await service.ResolveManyAsync(
        [
            new(TenantId, eventId, placements[0].Id, roomIds[0], null, EventLocationDisclosurePurpose.Management),
            new(TenantId, eventId, placements[1].Id, roomIds[1], RequesterUserId, EventLocationDisclosurePurpose.Management)
        ], CancellationToken.None);

        await Assert.That(calls.SequenceEqual(
            ["event-locations", "rooms", "governance", "management-authorization"])).IsTrue();
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.Values.All(item => item.LocationId is null)).IsTrue();
        await management.Received(1).AuthorizeManyAsync(
            Arg.Is<IReadOnlyCollection<EventLocation>>(items => items.Count == 2),
            EventLocationExactReadPurposeEnum.EventManagement,
            null,
            null,
            Arg.Any<CancellationToken>());
        await registrationRepository.DidNotReceive().GetLocationAccessCoverageAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Disclosure_RejectsOversizedOrCrossEventPrivateBatchesBeforeIo()
    {
        var locationRepository = Substitute.For<IEventLocationRepository>();
        var service = CreateDisclosureService(
            locationRepository,
            Substitute.For<ILocationRoomRepository>(),
            Substitute.For<IEventRegistrationRepository>(),
            Substitute.For<IEventLocationRegistrationAccessService>(),
            Substitute.For<ILocationPrivacyGovernanceService>(),
            Substitute.For<IEventLocationManagementAuthorizationService>(),
            CreateCurrentUser(RequesterUserId));
        EventLocationDisclosureRequest[] oversized = Enumerable.Range(
                0,
                IEventLocationDisclosureService.MaximumBatchSize + 1)
            .Select(_ => new EventLocationDisclosureRequest(
                TenantId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                null,
                null,
                EventLocationDisclosurePurpose.Public))
            .ToArray();

        await Assert.That(async () => await service.ResolveManyAsync(oversized, CancellationToken.None))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(async () => await service.ResolveManyAsync(
        [
            new(TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), null, null, EventLocationDisclosurePurpose.Management),
            new(TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), null, null, EventLocationDisclosurePurpose.Management)
        ], CancellationToken.None)).Throws<ArgumentException>();
        await Assert.That(async () => await service.ResolveManyAsync(
        [
            new(TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), null, Guid.CreateVersion7(), EventLocationDisclosurePurpose.Management)
        ], CancellationToken.None)).Throws<AuthorizationException>();
        await locationRepository.DidNotReceive().GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
    }

    private static EventLocationManagementAuthorizationService CreateManagementService(
        IEventRepository eventRepository,
        IAuthorizationProvider authorizationProvider,
        IEventLocationExactReadAuditService auditService,
        ICurrentUserService currentUserService) =>
        new(
            eventRepository,
            authorizationProvider,
            currentUserService,
            auditService,
            Substitute.For<ILogger<EventLocationManagementAuthorizationService>>());

    private static EventLocationDisclosureService CreateDisclosureService(
        IEventLocationRepository locationRepository,
        ILocationRoomRepository roomRepository,
        IEventRegistrationRepository registrationRepository,
        IEventLocationRegistrationAccessService registrationAccess,
        ILocationPrivacyGovernanceService governance,
        IEventLocationManagementAuthorizationService management,
        ICurrentUserService currentUser) =>
        new(
            locationRepository,
            roomRepository,
            registrationRepository,
            registrationAccess,
            governance,
            management,
            currentUser,
            new EventLocationDisclosureEvaluator(),
            new FixedTimeProvider(Now));

    private static ICurrentUserService CreateCurrentUser(Guid? userId)
    {
        var service = Substitute.For<ICurrentUserService>();
        service.UserId.Returns(userId);
        service.IsAuthenticated.Returns(userId.HasValue);
        return service;
    }

    private static EffectiveLocationPrivacyGovernance ResolvedGovernance() => new(
        IsResolved: true,
        LocationPrivacyGovernanceReasonCode.Resolved,
        AllowHomeLocations: true,
        AllowPublicExactAddress: true,
        AllowPublicCoordinates: true,
        MinimumHomeAudience: LocationDisclosureAudienceEnum.AnyCurrentRegistrant,
        DefaultRevealOffset: TimeSpan.Zero);

    private static EventLocation CreatePlacement(Guid eventId) => EventLocation.CreatePhysical(
        TenantId,
        eventId,
        Guid.CreateVersion7(),
        RequesterUserId,
        Now.UtcDateTime);

    private static EventLocation CreateTba(Guid eventId) => EventLocation.CreateToBeAnnounced(
        TenantId,
        eventId,
        RequesterUserId,
        Now.UtcDateTime);

    private static Explore.Domain.Event CreateEvent(Guid eventId) => new()
    {
        Id = eventId,
        TenantId = TenantId,
        Tenant = null!,
        ActorId = Guid.CreateVersion7(),
        Actor = null!,
        Title = $"Event {eventId:N}",
        EventStatusId = (int)EventStatusEnum.Draft,
        EventStatus = null!,
        EventFormatId = (int)EventFormatEnum.Local,
        EventFormat = null!,
        VisibilityTypeId = (int)VisibilityTypeEnum.Private,
        VisibilityType = null!
    };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
