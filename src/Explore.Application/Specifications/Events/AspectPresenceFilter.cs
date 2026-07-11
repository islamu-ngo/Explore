using System.Linq.Expressions;
using Explore.Domain;

namespace Explore.Application.Specifications.Events;

/// <summary>
/// Static factory for filtering events by aspect presence.
/// Useful for scoping results to events that belong to a specific module
/// (e.g., "show only Islamic events" or "show only Tech events").
/// </summary>
/// <remarks>
/// Aspect presence is determined by the existence of the 1:1 aspect record.
/// EF Core translates <c>e.IslamicAspect != null</c> to a LEFT JOIN + IS NOT NULL check.
/// </remarks>
public sealed class AspectPresenceFilter : IFilterSpecification<Event>
{
    public Expression<Func<Event, bool>> Predicate { get; }

    private AspectPresenceFilter(Expression<Func<Event, bool>> predicate) => Predicate = predicate;

    /// <summary>
    /// Filters events that have an Islamic aspect configured.
    /// </summary>
    public static AspectPresenceFilter HasIslamicAspect() =>
        new(e => e.IslamicAspect != null);

    /// <summary>
    /// Filters events that have a Tech aspect configured.
    /// </summary>
    public static AspectPresenceFilter HasTechAspect() =>
        new(e => e.TechAspect != null);

    /// <summary>
    /// Filters events that have both Islamic and Tech aspects configured.
    /// </summary>
    public static AspectPresenceFilter HasBothAspects() =>
        new(e => e.IslamicAspect != null && e.TechAspect != null);
}
