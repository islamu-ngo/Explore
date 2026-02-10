using System.Linq.Expressions;
using Explore.Domain;

namespace Explore.Application.Specifications.Events;

/// <summary>
/// Static factory for creating Islamic aspect filter specifications.
/// Each filter targets <see cref="EventIslamicAspect"/> properties via the Event navigation property.
/// These filters are only composed when the Islamic module is enabled for the current tenant.
/// </summary>
/// <remarks>
/// Usage (module-conditional):
/// <code>
/// if (await moduleService.IsModuleEnabledAsync(tenantId, "Mod_Islamic"))
/// {
///     spec = spec.And(IslamicAspectFilter.GenderMode(GenderSegregationMode.WomenOnly))
///                .And(IslamicAspectFilter.IncludesQuranRecitation());
/// }
/// </code>
/// </remarks>
public sealed class IslamicAspectFilter : IFilterSpecification<Event>
{
    public Expression<Func<Event, bool>> Predicate { get; }

    private IslamicAspectFilter(Expression<Func<Event, bool>> predicate) => Predicate = predicate;

    /// <summary>
    /// Filters events by Islamic aspect madhab (school of jurisprudence).
    /// Requires the Islamic aspect to exist on the event.
    /// </summary>
    public static IslamicAspectFilter Madhab(int madhabId) =>
        new(e => e.IslamicAspect != null && e.IslamicAspect.MadhabId == madhabId);

    /// <summary>
    /// Filters events by gender segregation mode.
    /// </summary>
    public static IslamicAspectFilter GenderMode(GenderSegregationMode genderMode) =>
        new(e => e.IslamicAspect != null && e.IslamicAspect.GenderMode == genderMode);

    /// <summary>
    /// Filters events that include Quran recitation.
    /// </summary>
    public static IslamicAspectFilter IncludesQuranRecitation() =>
        new(e => e.IslamicAspect != null && e.IslamicAspect.IncludesQuranRecitation);

    /// <summary>
    /// Filters events by reference prayer time for scheduling.
    /// </summary>
    public static IslamicAspectFilter ReferencePrayer(PrayerTime prayerTime) =>
        new(e => e.IslamicAspect != null && e.IslamicAspect.ReferencePrayer == prayerTime);

    /// <summary>
    /// Filters events by primary language of Islamic content.
    /// </summary>
    public static IslamicAspectFilter PrimaryLanguage(int languageId) =>
        new(e => e.IslamicAspect != null && e.IslamicAspect.PrimaryLanguageId == languageId);
}
