using System.Linq.Expressions;

namespace Explore.Application.Specifications;

/// <summary>
/// Represents a single filter criterion that can be applied to an <see cref="IQueryable{T}"/>.
/// Implementations provide an expression predicate for database-level filtering.
/// </summary>
/// <typeparam name="T">The entity type to filter.</typeparam>
public interface IFilterSpecification<T>
{
    /// <summary>
    /// Gets the filter predicate expression for EF Core query translation.
    /// </summary>
    Expression<Func<T, bool>> Predicate { get; }
}
