// ABOUTME: Specification filter for custom property projection-backed discovery queries (Layer 3).
// ABOUTME: Applied as DbContext-level subqueries against EventCustomPropertyProjections table.

namespace Explore.Application.Specifications.Events;

/// <summary>
/// Represents a projection-backed filter for custom property discovery queries.
/// These filters are applied at the repository level via correlated subqueries
/// against the <c>EventCustomPropertyProjections</c> DbSet.
/// </summary>
/// <remarks>
/// Layer 3 filters are gated behind the <c>custom_properties.projection_discovery_enabled</c>
/// tenant setting. They do NOT touch raw EAV value tables — only pre-computed projections.
/// </remarks>
public sealed class EventCustomPropertyProjectionFilter
{
    /// <summary>
    /// Gets the type of projection filter operation.
    /// </summary>
    public EventCustomPropertyProjectionFilterType FilterType { get; }

    /// <summary>
    /// Gets the filter value (varies by filter type).
    /// </summary>
    public object Value { get; }

    private EventCustomPropertyProjectionFilter(EventCustomPropertyProjectionFilterType filterType, object value)
    {
        FilterType = filterType;
        Value = value;
    }

    /// <summary>
    /// Filters events that have a custom property projection matching the specified namespace and key
    /// with a normalized value equal to the specified value (case-insensitive via NormalizedValue).
    /// Only matches projections where <c>IsFilterable = true</c>.
    /// </summary>
    public static EventCustomPropertyProjectionFilter ExactMatch(string @namespace, string key, string normalizedValue) =>
        new(EventCustomPropertyProjectionFilterType.ExactMatch, (Namespace: @namespace, Key: key, NormalizedValue: normalizedValue));

    /// <summary>
    /// Filters events that have a custom property projection matching the specified namespace and key
    /// with a specific option selected. Only matches projections where <c>IsFilterable = true</c>.
    /// </summary>
    public static EventCustomPropertyProjectionFilter OptionMatch(string @namespace, string key, Guid optionId) =>
        new(EventCustomPropertyProjectionFilterType.OptionMatch, (Namespace: @namespace, Key: key, OptionId: optionId));

    /// <summary>
    /// Filters events that have a custom property projection matching the specified namespace and key
    /// with any of the specified options selected (OR logic).
    /// Only matches projections where <c>IsFilterable = true</c>.
    /// </summary>
    public static EventCustomPropertyProjectionFilter OptionsMatchAny(string @namespace, string key, List<Guid> optionIds) =>
        new(EventCustomPropertyProjectionFilterType.OptionsMatchAny, (Namespace: @namespace, Key: key, OptionIds: optionIds));

    /// <summary>
    /// Filters events that have a custom property projection matching the specified namespace and key
    /// where the NormalizedValue contains the search term (ILIKE '%term%').
    /// Only matches projections where <c>IsSearchable = true</c>.
    /// </summary>
    public static EventCustomPropertyProjectionFilter TextSearch(string @namespace, string key, string searchTerm) =>
        new(EventCustomPropertyProjectionFilterType.TextSearch, (Namespace: @namespace, Key: key, SearchTerm: searchTerm));

    /// <summary>
    /// Filters events that have any custom property projection where <c>IsSearchable = true</c>
    /// and the NormalizedValue contains the search term (cross-property text search).
    /// </summary>
    public static EventCustomPropertyProjectionFilter GlobalTextSearch(string searchTerm) =>
        new(EventCustomPropertyProjectionFilterType.GlobalTextSearch, searchTerm);

    /// <summary>
    /// Filters events that have a custom property projection matching the specified namespace and key
    /// (existence check — the property has a value regardless of what it is).
    /// </summary>
    public static EventCustomPropertyProjectionFilter Exists(string @namespace, string key) =>
        new(EventCustomPropertyProjectionFilterType.Exists, (Namespace: @namespace, Key: key));

    /// <summary>
    /// Filters events that have a custom property projection matching the specified namespace and key
    /// with a boolean value of <c>true</c>.
    /// Only matches projections where <c>IsFilterable = true</c>.
    /// </summary>
    public static EventCustomPropertyProjectionFilter BooleanTrue(string @namespace, string key) =>
        new(EventCustomPropertyProjectionFilterType.BooleanTrue, (Namespace: @namespace, Key: key));

    /// <summary>
    /// Filters events that have a custom property projection matching the specified namespace and key
    /// with a numeric value within the specified range (inclusive).
    /// Only matches projections where <c>IsFilterable = true</c>.
    /// </summary>
    public static EventCustomPropertyProjectionFilter NumberRange(string @namespace, string key, decimal? min, decimal? max) =>
        new(EventCustomPropertyProjectionFilterType.NumberRange, (Namespace: @namespace, Key: key, Min: min, Max: max));

    /// <summary>
    /// Filters events that have a custom property projection matching the specified namespace and key
    /// with a date/time value within the specified range (inclusive).
    /// Only matches projections where <c>IsFilterable = true</c>.
    /// </summary>
    public static EventCustomPropertyProjectionFilter DateRange(string @namespace, string key, DateTimeOffset? from, DateTimeOffset? to) =>
        new(EventCustomPropertyProjectionFilterType.DateRange, (Namespace: @namespace, Key: key, From: from, To: to));
}

/// <summary>
/// Enumeration of projection filter types for custom property discovery queries.
/// Each type corresponds to a different subquery pattern in the repository.
/// </summary>
public enum EventCustomPropertyProjectionFilterType
{
    /// <summary>Exact match on NormalizedValue where IsFilterable = true.</summary>
    ExactMatch,

    /// <summary>Option match on OptionId where IsFilterable = true.</summary>
    OptionMatch,

    /// <summary>Any-of-options match on OptionId where IsFilterable = true (OR logic).</summary>
    OptionsMatchAny,

    /// <summary>Text search on NormalizedValue where IsSearchable = true (ILIKE).</summary>
    TextSearch,

    /// <summary>Cross-property text search on any searchable projection (ILIKE).</summary>
    GlobalTextSearch,

    /// <summary>Existence check — property has a projection row.</summary>
    Exists,

    /// <summary>Boolean true filter where IsFilterable = true.</summary>
    BooleanTrue,

    /// <summary>Numeric range filter where IsFilterable = true.</summary>
    NumberRange,

    /// <summary>Date/time range filter where IsFilterable = true.</summary>
    DateRange
}
