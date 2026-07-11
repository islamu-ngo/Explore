using System.Linq.Expressions;

namespace Explore.Application.Specifications;

/// <summary>
/// Represents a sort criterion that can be applied to an <see cref="IQueryable{T}"/>.
/// Implementations provide a key selector expression for database-level ordering.
/// </summary>
/// <typeparam name="T">The entity type to sort.</typeparam>
public interface ISortSpecification<T>
{
    /// <summary>
    /// Gets the sort key selector expression for EF Core query translation.
    /// </summary>
    Expression<Func<T, object>> KeySelector { get; }
}
