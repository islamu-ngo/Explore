using System.Linq.Expressions;
using Explore.Domain;

namespace Explore.Application.Specifications.Events;

/// <summary>
/// Static factory for creating event filter specifications.
/// Each filter translates to a database-level WHERE clause via EF Core expression trees.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// var query = new EventQuerySpecification()
///     .And(EventFilter.Category(categoryId))
///     .And(EventFilter.SearchTerm("workshop"))
///     .And(EventFilter.DateFrom(DateOnly.FromDateTime(DateTime.Today)));
/// </code>
/// </remarks>
public sealed class EventFilter : IFilterSpecification<Event>
{
    public Expression<Func<Event, bool>> Predicate { get; }

    private EventFilter(Expression<Func<Event, bool>> predicate) => Predicate = predicate;

    /// <summary>
    /// Filters events by event type.
    /// </summary>
    public static EventFilter EventType(int eventTypeId) =>
        new(e => e.EventTypeId == eventTypeId);

    /// <summary>
    /// Filters events by event format (online, in-person, hybrid).
    /// </summary>
    public static EventFilter Format(int formatId) =>
        new(e => e.EventFormatId == formatId);

    /// <summary>
    /// Filters events by madhab.
    /// </summary>
    public static EventFilter Madhab(int madhabId) =>
        new(e => e.MadhabId == madhabId);

    /// <summary>
    /// Filters events by audience gender.
    /// </summary>
    public static EventFilter AudienceGender(int audienceGenderId) =>
        new(e => e.AudienceGenderId == audienceGenderId);

    /// <summary>
    /// Filters events by audience age.
    /// </summary>
    public static EventFilter AudienceAge(int audienceAgeId) =>
        new(e => e.AudienceAgeId == audienceAgeId);

    /// <summary>
    /// Filters events by event status.
    /// </summary>
    public static EventFilter Status(int statusId) =>
        new(e => e.EventStatusId == statusId);

    /// <summary>
    /// Filters events by visibility type.
    /// </summary>
    public static EventFilter Visibility(int visibilityTypeId) =>
        new(e => e.VisibilityTypeId == visibilityTypeId);

    /// <summary>
    /// Filters events by actor (organizer).
    /// </summary>
    public static EventFilter Actor(Guid actorId) =>
        new(e => e.ActorId == actorId);

    /// <summary>
    /// Searches events by title or description (case-insensitive).
    /// Translates to SQL ILIKE/LIKE for database-level search.
    /// </summary>
    public static EventFilter SearchTerm(string searchTerm) =>
        new(e => e.Title.Contains(searchTerm) ||
                 (e.Description != null && e.Description.Contains(searchTerm)));

    /// <summary>
    /// Filters events with first session date on or after the specified date.
    /// </summary>
    public static EventFilter DateFrom(DateOnly dateFrom) =>
        new(e => e.FirstSessionDate != null && e.FirstSessionDate >= dateFrom);

    /// <summary>
    /// Filters events with first session date on or before the specified date.
    /// </summary>
    public static EventFilter DateTo(DateOnly dateTo) =>
        new(e => e.FirstSessionDate != null && e.FirstSessionDate <= dateTo);

    /// <summary>
    /// Filters events that have a specific category assigned.
    /// Requires a subquery on EventCategories — use <see cref="EventSubqueryFilter"/> instead.
    /// </summary>
    /// <remarks>
    /// Category and Tag filtering require access to junction tables (EventCategory, EventTag)
    /// which are not directly navigable from the Event entity. These filters are applied
    /// at the repository level using DbContext-aware subqueries via <see cref="EventSubqueryFilter"/>.
    /// </remarks>
    public static EventFilter Free() =>
        new(e => e.Price == null || e.Price == 0);

    /// <summary>
    /// Filters events that require registration.
    /// </summary>
    public static EventFilter RegistrationRequired() =>
        new(e => e.IsRegistrationRequired);
}
