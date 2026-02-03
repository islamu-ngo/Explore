// ABOUTME: Extension methods for selectively ignoring named query filters in EF Core 10+ queries.
// Provides fluent API for including soft-deleted entities or querying across tenants.

namespace Explore.Persistence.QueryFilters;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Extension methods for selectively ignoring named query filters.
/// Uses EF Core 10+ IgnoreQueryFilters(string[]) syntax for selective filter disabling.
/// </summary>
public static class QueryFilterExtensions
{
    /// <summary>
    /// Ignores the soft delete filter, including soft-deleted entities in the query.
    /// Tenant isolation is still enforced.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The queryable to modify.</param>
    /// <returns>The queryable with the soft delete filter disabled.</returns>
    public static IQueryable<TEntity> IncludeDeleted<TEntity>(this IQueryable<TEntity> query)
        where TEntity : class
    {
        return query.IgnoreQueryFilters([QueryFilterNames.SoftDelete]);
    }

    /// <summary>
    /// Ignores the tenant filter, querying across all tenants.
    /// WARNING: Use only for admin operations. This exposes data from all tenants.
    /// Soft delete filter is still enforced.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The queryable to modify.</param>
    /// <returns>The queryable with the tenant filter disabled.</returns>
    public static IQueryable<TEntity> IgnoreTenantFilter<TEntity>(this IQueryable<TEntity> query)
        where TEntity : class
    {
        return query.IgnoreQueryFilters([QueryFilterNames.Tenant]);
    }

    /// <summary>
    /// Ignores all query filters (tenant and soft delete).
    /// WARNING: Use only for admin operations requiring full data access.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The queryable to modify.</param>
    /// <returns>The queryable with all filters disabled.</returns>
    public static IQueryable<TEntity> IgnoreAllFilters<TEntity>(this IQueryable<TEntity> query)
        where TEntity : class
    {
        return query.IgnoreQueryFilters();
    }
}
