namespace Explore.Application.Responses;

/// <summary>
/// Represents a paginated result set for list queries.
/// Follows Microsoft's recommended PaginatedList pattern with Skip/Take pagination.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// Default page size when not specified.
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// Maximum allowed page size to prevent excessive queries.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Gets or sets the items in the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

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
    /// Initializes a new instance of the <see cref="PaginatedResult{T}"/> class.
    /// </summary>
    public PaginatedResult()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginatedResult{T}"/> class with the specified values.
    /// </summary>
    /// <param name="items">The items in the current page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    public PaginatedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Creates a new <see cref="PaginatedResult{T}"/> instance with validated parameters.
    /// Page size is clamped to MaxPageSize (100) to prevent excessive queries.
    /// Page number is ensured to be at least 1.
    /// </summary>
    /// <param name="items">The items in the current page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <param name="pageNumber">The current page number (will be set to 1 if less than 1).</param>
    /// <param name="pageSize">The number of items per page (will be clamped to MaxPageSize if greater).</param>
    /// <returns>A new <see cref="PaginatedResult{T}"/> instance.</returns>
    public static PaginatedResult<T> Create(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        // Validate and clamp parameters
        var validatedPageNumber = Math.Max(1, pageNumber);
        var validatedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        return new PaginatedResult<T>(items, totalCount, validatedPageNumber, validatedPageSize);
    }

    /// <summary>
    /// Normalizes pagination parameters to valid ranges.
    /// Use this in handlers before calling repository methods.
    /// </summary>
    /// <param name="pageNumber">The requested page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>A tuple with validated (pageNumber, pageSize).</returns>
    public static (int PageNumber, int PageSize) NormalizeParameters(int pageNumber, int pageSize)
    {
        return (Math.Max(1, pageNumber), Math.Clamp(pageSize, 1, MaxPageSize));
    }
}
