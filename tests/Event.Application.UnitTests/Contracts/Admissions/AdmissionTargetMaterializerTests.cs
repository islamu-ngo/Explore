// ABOUTME: Verifies catalog publication materializes exact reusable admission targets and default policies.
// ABOUTME: Covers event/day/session bounds, idempotence, retired scope safety, and fail-closed schedules.

using Explore.Application.Contracts.Admissions;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Contracts.Admissions;

public sealed class AdmissionTargetMaterializerTests
{
    [Test]
    public async Task MaterializeAsync_CreatesEventDayAndSessionTargetsWithExactSingleEntryWindows()
    {
        Scenario scenario = CreateScenario(includeAllScopes: true);
        IAdmissionTargetMaterializationRepository repository = Repository(scenario.Sessions);
        AdmissionTarget[] addedTargets = [];
        AdmissionCheckInPolicy[] addedPolicies = [];
        repository.AddTargetsAsync(Arg.Do<IReadOnlyCollection<AdmissionTarget>>(values => addedTargets = values.ToArray()), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        repository.AddPoliciesAsync(Arg.Do<IReadOnlyCollection<AdmissionCheckInPolicy>>(values => addedPolicies = values.ToArray()), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await new AdmissionTargetMaterializer(repository).MaterializeAsync(
            scenario.Event,
            scenario.Catalog,
            CancellationToken.None);

        await Assert.That(addedTargets.Length).IsEqualTo(3);
        await Assert.That(addedPolicies.Length).IsEqualTo(3);
        AdmissionTarget eventTarget = addedTargets.Single(target => target.AdmissionTargetTypeId == (int)AdmissionTargetTypeEnum.Event);
        AdmissionTarget dayTarget = addedTargets.Single(target => target.AdmissionTargetTypeId == (int)AdmissionTargetTypeEnum.EventDay);
        AdmissionTarget sessionTarget = addedTargets.Single(target => target.AdmissionTargetTypeId == (int)AdmissionTargetTypeEnum.EventSession);
        await Assert.That(dayTarget.EventDayId).IsEqualTo(scenario.DayId);
        await Assert.That(sessionTarget.EventSessionId).IsEqualTo(scenario.SessionId);

        AdmissionCheckInPolicy eventPolicy = addedPolicies.Single(policy => policy.AdmissionTargetId == eventTarget.Id);
        AdmissionCheckInPolicy dayPolicy = addedPolicies.Single(policy => policy.AdmissionTargetId == dayTarget.Id);
        AdmissionCheckInPolicy sessionPolicy = addedPolicies.Single(policy => policy.AdmissionTargetId == sessionTarget.Id);
        await Assert.That(eventPolicy.OpensAtUtc).IsEqualTo(new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Utc));
        await Assert.That(eventPolicy.ClosesAtUtc).IsEqualTo(new DateTime(2026, 9, 11, 17, 0, 0, DateTimeKind.Utc));
        await Assert.That(dayPolicy.OpensAtUtc).IsEqualTo(new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Utc));
        await Assert.That(dayPolicy.ClosesAtUtc).IsEqualTo(new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc));
        await Assert.That(sessionPolicy.OpensAtUtc).IsEqualTo(new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Utc));
        await Assert.That(sessionPolicy.ClosesAtUtc).IsEqualTo(new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc));
        await Assert.That(addedPolicies.All(policy => policy.MaximumEntries == 1)).IsTrue();
    }

    [Test]
    public async Task MaterializeAsync_WhenCurrentScopeExists_ReusesRowsAndLeavesRetiredScopeRowsUntouched()
    {
        Scenario scenario = CreateScenario(includeAllScopes: false);
        AdmissionTarget current = AdmissionTarget.Create(
            Guid.CreateVersion7(), scenario.TenantId, scenario.EventId,
            AdmissionTargetTypeEnum.Event, null, null);
        AdmissionCheckInPolicy currentPolicy = AdmissionCheckInPolicy.Create(
            Guid.CreateVersion7(), current,
            new DateTime(2026, 9, 10, 8, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 11, 17, 30, 0, DateTimeKind.Utc),
            maximumEntries: 2);
        AdmissionTarget retiredScope = AdmissionTarget.Create(
            Guid.CreateVersion7(), scenario.TenantId, scenario.EventId,
            AdmissionTargetTypeEnum.EventDay, scenario.DayId, null);
        AdmissionCheckInPolicy retiredPolicy = AdmissionCheckInPolicy.Create(
            Guid.CreateVersion7(), retiredScope,
            new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            maximumEntries: 1);
        IAdmissionTargetMaterializationRepository repository = Repository(
            scenario.Sessions,
            [current, retiredScope],
            [currentPolicy, retiredPolicy]);

        await new AdmissionTargetMaterializer(repository).MaterializeAsync(
            scenario.Event,
            scenario.Catalog,
            CancellationToken.None);

        await repository.DidNotReceive().AddTargetsAsync(
            Arg.Any<IReadOnlyCollection<AdmissionTarget>>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().AddPoliciesAsync(
            Arg.Any<IReadOnlyCollection<AdmissionCheckInPolicy>>(), Arg.Any<CancellationToken>());
        await Assert.That(currentPolicy.MaximumEntries).IsEqualTo(2);
        await Assert.That(retiredScope.IsOperational).IsTrue();
    }

    [Test]
    public async Task MaterializeAsync_WhenScheduleBoundsAreIncomplete_FailsBeforeStagingRows()
    {
        Scenario scenario = CreateScenario(includeAllScopes: false);
        scenario.Sessions[1].EndTime = null;
        IAdmissionTargetMaterializationRepository repository = Repository(scenario.Sessions);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            new AdmissionTargetMaterializer(repository).MaterializeAsync(
                scenario.Event,
                scenario.Catalog,
                CancellationToken.None));

        await Assert.That(exception.Message).Contains("complete UTC schedule bounds");
        await repository.DidNotReceive().AddTargetsAsync(
            Arg.Any<IReadOnlyCollection<AdmissionTarget>>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceive().AddPoliciesAsync(
            Arg.Any<IReadOnlyCollection<AdmissionCheckInPolicy>>(), Arg.Any<CancellationToken>());
    }

    private static IAdmissionTargetMaterializationRepository Repository(
        IReadOnlyList<EventSession> sessions,
        IReadOnlyList<AdmissionTarget>? targets = null,
        IReadOnlyList<AdmissionCheckInPolicy>? policies = null)
    {
        IAdmissionTargetMaterializationRepository repository = Substitute.For<IAdmissionTargetMaterializationRepository>();
        repository.ListScheduleSessionsForUpdateAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(sessions);
        repository.ListTargetsForUpdateAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(targets ?? []);
        repository.ListPoliciesAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(policies ?? []);
        return repository;
    }

    private static Scenario CreateScenario(bool includeAllScopes)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid dayId = Guid.CreateVersion7();
        Guid sessionId = Guid.CreateVersion7();
        DomainEvent eventTarget = new()
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Admission schedule",
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };
        EventDay day = new()
        {
            Id = dayId,
            EventId = eventId,
            TenantId = tenantId,
            LocalDate = new DateOnly(2026, 9, 10),
            Event = eventTarget,
            Tenant = null!
        };
        EventSession first = new()
        {
            Id = sessionId,
            EventId = eventId,
            EventDayId = dayId,
            TenantId = tenantId,
            StartTime = new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero),
            Event = eventTarget,
            Tenant = null!
        };
        EventSession second = new()
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            TenantId = tenantId,
            StartTime = new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 11, 17, 0, 0, TimeSpan.Zero),
            Event = eventTarget,
            Tenant = null!
        };
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(tenantId, eventId, "USD", 1);
        EventTicketType ticketType = EventTicketType.Create(
            Guid.CreateVersion7(), tenantId, catalog.Id, "General admission", "USD",
            TicketPricingModeEnum.Free, null, null, null,
            ParticipantDataCollectionModeEnum.None, null, null, null, false, false,
            null, null, null, null);
        catalog.AddTicketType(ticketType, null);
        catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEvent(ticketType.Id, tenantId, eventId, 1));
        if (includeAllScopes)
        {
            catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEventDay(
                ticketType.Id, day, 1, EntitlementSelectionRuleEnum.FixedSelection));
            catalog.AddEntitlement(ticketType, TicketTypeEntitlement.CreateForEventSession(
                ticketType.Id, first, 1, EntitlementSelectionRuleEnum.FixedSelection));
        }

        return new Scenario(tenantId, eventId, dayId, sessionId, eventTarget, catalog, [first, second]);
    }

    private sealed record Scenario(
        Guid TenantId,
        Guid EventId,
        Guid DayId,
        Guid SessionId,
        DomainEvent Event,
        EventTicketCatalogVersion Catalog,
        EventSession[] Sessions);
}
