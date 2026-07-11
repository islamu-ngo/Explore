// ABOUTME: Presentation-only pagination state composed from generated collection resources.
// ABOUTME: Keeps UI paging calculations without duplicating an API payload contract.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Models;

/// <summary>
/// Represents a paginated result set for Blazor UI consumption.
/// Normalizes generated paginated and HAL responses for UI list state.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public sealed class PaginatedResult<T>
{
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public required List<T> Items { get; init; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public required int TotalCount { get; init; }

    public IReadOnlyDictionary<string, HalLink>? Links { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Creates an empty paginated result.
    /// </summary>
    public static PaginatedResult<T> Empty(int pageNumber = 1, int pageSize = 20) => new()
    {
        Items = [],
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalCount = 0
    };

    public bool HasHalLink(string rel) =>
        !string.IsNullOrWhiteSpace(rel) && Links?.ContainsKey(rel) == true;
}
