namespace Explore.Application.Specifications;

/// <summary>
/// Composes filter and sort specifications into a single query specification
/// that can be applied to an <see cref="IQueryable{T}"/>.
/// Follows the fluent builder pattern for composable, readable query construction.
/// </summary>
/// <typeparam name="T">The entity type to query.</typeparam>
/// <example>
/// <code>
/// var query = new EventQuerySpecification()
///     .And(EventFilter.Category(categoryId))
///     .And(EventFilter.SearchTerm("workshop"))
///     .SortByDescending(EventSort.Date);
/// </code>
/// </example>
public interface IQuerySpecification<T>
{
    /// <summary>
    /// Gets the filter specifications composed into this query.
    /// </summary>
    IReadOnlyList<IFilterSpecification<T>> Filters { get; }

    /// <summary>
    /// Gets the sort specification, if any.
    /// </summary>
    ISortSpecification<T>? Sort { get; }

    /// <summary>
    /// Gets whether the sort is descending.
    /// </summary>
    bool SortDescending { get; }

    /// <summary>
    /// Adds a filter specification (AND composition).
    /// </summary>
    IQuerySpecification<T> And(IFilterSpecification<T> filter);

    /// <summary>
    /// Sets the sort specification in ascending order.
    /// </summary>
    IQuerySpecification<T> SortBy(ISortSpecification<T> sort);

    /// <summary>
    /// Sets the sort specification in descending order.
    /// </summary>
    IQuerySpecification<T> SortByDescending(ISortSpecification<T> sort);

    /// <summary>
    /// Applies all filters and sorting to the given queryable.
    /// </summary>
    /// <param name="query">The EF Core queryable to apply specifications to.</param>
    /// <returns>The queryable with all filter and sort specifications applied.</returns>
    IQueryable<T> Apply(IQueryable<T> query);

    /// <summary>
    /// Gets whether any filters have been added.
    /// </summary>
    bool HasFilters { get; }

    /// <summary>
    /// Gets whether a sort has been specified.
    /// </summary>
    bool HasSort { get; }
}
