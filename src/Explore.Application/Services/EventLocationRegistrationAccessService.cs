// ABOUTME: Resolves live registration intents and child placements into fail-closed EventLocation access facts.
// ABOUTME: Applies identity, scope, lifecycle, null-mode, requested-placement, and audience rules to pure or loaded facts.

using System.Collections.Immutable;
using Explore.Application.Contracts.Services;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Services;

public sealed class EventLocationRegistrationAccessService : IEventLocationRegistrationAccessService
{
    public IReadOnlyDictionary<Guid, EventLocationRegistrationAccess> ResolveMany(
        Guid tenantId,
        Guid eventId,
        Guid userId,
        DateTimeOffset asOfUtc,
        IReadOnlyCollection<Guid> requestedEventLocationIds,
        IReadOnlyCollection<EventRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(requestedEventLocationIds);
        ArgumentNullException.ThrowIfNull(registrations);

        if (tenantId == Guid.Empty
            || eventId == Guid.Empty
            || userId == Guid.Empty
            || asOfUtc == default
            || asOfUtc.Offset != TimeSpan.Zero)
        {
            return new Dictionary<Guid, EventLocationRegistrationAccess>();
        }

        var requestedIds = requestedEventLocationIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Order()
            .ToArray();
        if (requestedIds.Length == 0)
        {
            return new Dictionary<Guid, EventLocationRegistrationAccess>();
        }

        var intentSources = registrations
            .Where(registration => HasValidLoadedSource(
                registration,
                tenantId,
                eventId,
                userId))
            .GroupBy(registration => registration.EventRegistrationIntentId!.Value)
            .Select(CreateLoadedIntentSource)
            .ToArray();
        if (intentSources.Length == 0)
        {
            return new Dictionary<Guid, EventLocationRegistrationAccess>();
        }

        var results = new Dictionary<Guid, EventLocationRegistrationAccess>(requestedIds.Length);
        foreach (var requestedId in requestedIds)
        {
            var strongest = intentSources
                .Select(source => Resolve(new(
                    requestedId,
                    asOfUtc,
                    source.Intent,
                    source.Coverage)))
                .OrderByDescending(access => access.CoversRequestedEventLocation)
                .ThenByDescending(access => AuthorityRank(access.EffectiveState))
                .ThenBy(access => access.IntentId)
                .First();
            results.Add(requestedId, strongest);
        }

        return results;
    }

    public EventLocationRegistrationAccess Resolve(EventLocationRegistrationAccessRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var intent = request.Intent;
        if (!HasValidIdentity(request))
        {
            return NoAccess(request, EventLocationRegistrationEffectiveState.Denied);
        }

        if (!IsParentLive(intent, request.AsOfUtc))
        {
            return NoAccess(request, EventLocationRegistrationEffectiveState.NonLive);
        }

        var parentState = ResolveParentState(intent.ApprovalStatusId);
        if (parentState is not null && !HasAudienceAuthority(parentState.Value))
        {
            return NoAccess(request, parentState.Value);
        }

        var resolvedCoverage = request.Coverage
            .Where(item => IsCoverageInScope(item, intent))
            .Select(item => new ResolvedCoverage(
                item,
                ResolveEffectiveCoverageState(item, request.AsOfUtc, parentState)))
            .ToArray();
        var liveCoverage = resolvedCoverage
            .Where(item => HasAudienceAuthority(item.State))
            .ToArray();

        if (liveCoverage.Length == 0)
        {
            return NoAccess(request, ResolveRequestedTerminalState(resolvedCoverage, request.RequestedEventLocationId));
        }

        var requestedCoverage = liveCoverage
            .Where(item => item.Fact.EventLocationId == request.RequestedEventLocationId)
            .ToArray();
        var effectiveState = requestedCoverage.Length == 0
            ? ResolveRequestedTerminalState(resolvedCoverage, request.RequestedEventLocationId)
            : requestedCoverage.MaxBy(item => AuthorityRank(item.State))!.State;

        return CreateAccess(
            intent.IntentId,
            intent.Scope,
            effectiveState,
            intent.EventId,
            intent.Scope == RegistrationScopeEnum.Event,
            intent.Scope == RegistrationScopeEnum.Day ? intent.SelectedEventDayId : null,
            liveCoverage
                .Select(item => item.Fact.EventSessionId)
                .Distinct()
                .Order()
                .ToImmutableArray(),
            request.RequestedEventLocationId,
            requestedCoverage.Length > 0);
    }

    private static bool HasValidLoadedSource(
        EventRegistration registration,
        Guid tenantId,
        Guid eventId,
        Guid userId)
    {
        var intent = registration.EventRegistrationIntent;
        var session = registration.EventSession;
        return registration.TenantId == tenantId
            && registration.EventId == eventId
            && registration.UserId == userId
            && registration.EventRegistrationIntentId is { } intentId
            && intent is not null
            && intent.Id == intentId
            && intent.TenantId == tenantId
            && intent.EventId == eventId
            && intent.UserId == userId
            && session is not null
            && session.Id == registration.EventSessionId
            && session.TenantId == tenantId
            && session.EventId == eventId
            && session.EventLocationId is { } eventLocationId
            && eventLocationId != Guid.Empty;
    }

    private static LoadedIntentSource CreateLoadedIntentSource(
        IGrouping<Guid, EventRegistration> registrations)
    {
        var rows = registrations.ToArray();
        var intent = rows[0].EventRegistrationIntent!;
        var intentFact = new EventLocationRegistrationIntentFact(
            intent.Id,
            intent.EventId,
            (RegistrationScopeEnum)intent.RegistrationScopeId,
            intent.SelectedEventDayId,
            intent.ApprovalStatusId,
            intent.IsDeleted,
            ExpiresAtUtc: null);
        var coverage = rows
            .Select(registration => new EventLocationRegistrationCoverageFact(
                intent.Id,
                registration.EventId,
                registration.EventSession.EventDayId,
                registration.EventSessionId,
                registration.EventSession.EventLocationId!.Value,
                registration.ApprovalStatusId,
                registration.EventSession.RegistrationModeId,
                registration.IsDeleted || registration.EventSession.IsDeleted,
                ExpiresAtUtc: null))
            .ToImmutableArray();
        return new(intentFact, coverage);
    }

    private static bool HasValidIdentity(EventLocationRegistrationAccessRequest request)
    {
        var intent = request.Intent;
        return request.RequestedEventLocationId != Guid.Empty
            && request.AsOfUtc != default
            && intent.IntentId != Guid.Empty
            && intent.EventId != Guid.Empty
            && Enum.IsDefined(intent.Scope)
            && (intent.Scope != RegistrationScopeEnum.Day || intent.SelectedEventDayId is { } dayId && dayId != Guid.Empty)
            && !request.Coverage.IsDefault;
    }

    private static bool IsParentLive(EventLocationRegistrationIntentFact intent, DateTimeOffset asOfUtc)
        => !intent.IsDeleted
            && (!intent.ExpiresAtUtc.HasValue || intent.ExpiresAtUtc.Value > asOfUtc);

    private static bool IsCoverageInScope(
        EventLocationRegistrationCoverageFact coverage,
        EventLocationRegistrationIntentFact intent)
    {
        if (coverage.IntentId != intent.IntentId
            || coverage.EventId != intent.EventId
            || coverage.EventSessionId == Guid.Empty
            || coverage.EventLocationId == Guid.Empty)
        {
            return false;
        }

        return intent.Scope switch
        {
            RegistrationScopeEnum.Event => true,
            RegistrationScopeEnum.Day => coverage.EventDayId == intent.SelectedEventDayId,
            RegistrationScopeEnum.SessionSelection => true,
            _ => false
        };
    }

    private static EventLocationRegistrationEffectiveState ResolveEffectiveCoverageState(
        EventLocationRegistrationCoverageFact coverage,
        DateTimeOffset asOfUtc,
        EventLocationRegistrationEffectiveState? parentState)
    {
        if (coverage.IsDeleted || coverage.ExpiresAtUtc.HasValue && coverage.ExpiresAtUtc.Value <= asOfUtc)
        {
            return EventLocationRegistrationEffectiveState.NonLive;
        }

        return ApplyParentCeiling(
            ResolveCoverageState(coverage.ApprovalStatusId, coverage.RegistrationModeId),
            parentState);
    }

    private static EventLocationRegistrationEffectiveState? ResolveParentState(int? approvalStatusId)
        => approvalStatusId switch
        {
            null => null,
            (int)ApprovalStatusEnum.Approved => EventLocationRegistrationEffectiveState.Confirmed,
            (int)ApprovalStatusEnum.Pending => EventLocationRegistrationEffectiveState.Pending,
            (int)ApprovalStatusEnum.Waitlisted => EventLocationRegistrationEffectiveState.Waitlisted,
            (int)ApprovalStatusEnum.Rejected => EventLocationRegistrationEffectiveState.Rejected,
            (int)ApprovalStatusEnum.Cancelled => EventLocationRegistrationEffectiveState.Cancelled,
            (int)ApprovalStatusEnum.Revoked => EventLocationRegistrationEffectiveState.Revoked,
            _ => EventLocationRegistrationEffectiveState.Denied
        };

    private static EventLocationRegistrationEffectiveState ResolveCoverageState(
        int? approvalStatusId,
        int? registrationModeId)
        => approvalStatusId switch
        {
            (int)ApprovalStatusEnum.Approved => EventLocationRegistrationEffectiveState.Confirmed,
            (int)ApprovalStatusEnum.Pending => EventLocationRegistrationEffectiveState.Pending,
            (int)ApprovalStatusEnum.Waitlisted => EventLocationRegistrationEffectiveState.Waitlisted,
            (int)ApprovalStatusEnum.Rejected => EventLocationRegistrationEffectiveState.Rejected,
            (int)ApprovalStatusEnum.Cancelled => EventLocationRegistrationEffectiveState.Cancelled,
            (int)ApprovalStatusEnum.Revoked => EventLocationRegistrationEffectiveState.Revoked,
            null => registrationModeId switch
            {
                (int)RegistrationModeEnum.Open => EventLocationRegistrationEffectiveState.Confirmed,
                (int)RegistrationModeEnum.ApprovalRequired => EventLocationRegistrationEffectiveState.Pending,
                _ => EventLocationRegistrationEffectiveState.Denied
            },
            _ => EventLocationRegistrationEffectiveState.Denied
        };

    private static EventLocationRegistrationEffectiveState ApplyParentCeiling(
        EventLocationRegistrationEffectiveState childState,
        EventLocationRegistrationEffectiveState? parentState)
    {
        if (!HasAudienceAuthority(childState) || parentState is null)
        {
            return childState;
        }

        return parentState == EventLocationRegistrationEffectiveState.Confirmed
            ? childState
            : parentState.Value;
    }

    private static int AuthorityRank(EventLocationRegistrationEffectiveState state)
        => state switch
        {
            EventLocationRegistrationEffectiveState.Confirmed => 2,
            EventLocationRegistrationEffectiveState.Pending or EventLocationRegistrationEffectiveState.Waitlisted => 1,
            _ => 0
        };

    private static bool HasAudienceAuthority(EventLocationRegistrationEffectiveState state)
        => state is EventLocationRegistrationEffectiveState.Pending
            or EventLocationRegistrationEffectiveState.Waitlisted
            or EventLocationRegistrationEffectiveState.Confirmed;

    private static EventLocationRegistrationEffectiveState ResolveRequestedTerminalState(
        IReadOnlyCollection<ResolvedCoverage> coverage,
        Guid requestedEventLocationId)
        => coverage
            .Where(item => item.Fact.EventLocationId == requestedEventLocationId)
            .Select(item => item.State)
            .OrderByDescending(TerminalEvidenceRank)
            .FirstOrDefault(EventLocationRegistrationEffectiveState.Denied);

    private static int TerminalEvidenceRank(EventLocationRegistrationEffectiveState state)
        => state switch
        {
            EventLocationRegistrationEffectiveState.NonLive => 4,
            EventLocationRegistrationEffectiveState.Revoked => 3,
            EventLocationRegistrationEffectiveState.Cancelled => 2,
            EventLocationRegistrationEffectiveState.Rejected => 1,
            _ => 0
        };

    private static EventLocationRegistrationAccess NoAccess(
        EventLocationRegistrationAccessRequest request,
        EventLocationRegistrationEffectiveState effectiveState)
        => CreateAccess(
            request.Intent.IntentId,
            request.Intent.Scope,
            effectiveState,
            request.Intent.EventId,
            false,
            null,
            [],
            request.RequestedEventLocationId,
            false);

    private static EventLocationRegistrationAccess CreateAccess(
        Guid intentId,
        RegistrationScopeEnum scope,
        EventLocationRegistrationEffectiveState effectiveState,
        Guid eventId,
        bool coversWholeEvent,
        Guid? coveredEventDayId,
        ImmutableArray<Guid> coveredEventSessionIds,
        Guid requestedEventLocationId,
        bool coversRequestedEventLocation)
        => new(
            intentId,
            scope,
            effectiveState,
            eventId,
            coversWholeEvent,
            coveredEventDayId,
            coveredEventSessionIds,
            requestedEventLocationId,
            coversRequestedEventLocation);

    private sealed record ResolvedCoverage(
        EventLocationRegistrationCoverageFact Fact,
        EventLocationRegistrationEffectiveState State);

    private sealed record LoadedIntentSource(
        EventLocationRegistrationIntentFact Intent,
        ImmutableArray<EventLocationRegistrationCoverageFact> Coverage);
}
