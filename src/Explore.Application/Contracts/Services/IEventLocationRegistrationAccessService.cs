// ABOUTME: Pure Application contract for resolving registration-based access to an EventLocation.
// ABOUTME: Accepts immutable loaded facts so ELP-225 can add repository-backed loading without DTO-returning repositories.

using System.Collections.Immutable;
using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Services;

public interface IEventLocationRegistrationAccessService
{
    EventLocationRegistrationAccess Resolve(EventLocationRegistrationAccessRequest request);
}

public sealed record EventLocationRegistrationAccessRequest(
    Guid RequestedEventLocationId,
    DateTimeOffset AsOfUtc,
    EventLocationRegistrationIntentFact Intent,
    ImmutableArray<EventLocationRegistrationCoverageFact> Coverage);

public sealed record EventLocationRegistrationIntentFact(
    Guid IntentId,
    Guid EventId,
    RegistrationScopeEnum Scope,
    Guid? SelectedEventDayId,
    int? ApprovalStatusId,
    bool IsDeleted,
    DateTimeOffset? ExpiresAtUtc);

public sealed record EventLocationRegistrationCoverageFact(
    Guid IntentId,
    Guid EventId,
    Guid? EventDayId,
    Guid EventSessionId,
    Guid EventLocationId,
    int? ApprovalStatusId,
    int? RegistrationModeId,
    bool IsDeleted,
    DateTimeOffset? ExpiresAtUtc);
