namespace Explore.Application.Hateoas;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a HAL collection resource with pagination metadata.
/// Items are embedded under "_embedded.items" per HAL convention.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public sealed class HalCollectionResource<T> where T : class
{
    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; init; }

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    [JsonPropertyName("hasPrevious")]
    public bool HasPrevious => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    [JsonPropertyName("hasNext")]
    public bool HasNext => PageNumber < TotalPages;

    /// <summary>
    /// HAL links for navigation (self, first, prev, next, last, create).
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; init; } = new();

    /// <summary>
    /// Embedded items. Contains "items" key with the collection items.
    /// </summary>
    [JsonPropertyName("_embedded")]
    public HalCollectionEmbedded<T> Embedded { get; init; } = new();

    /// <summary>
    /// Creates an empty collection resource.
    /// </summary>
    public HalCollectionResource() { }

    /// <summary>
    /// Creates a collection resource from a list of HAL resources.
    /// </summary>
    public static HalCollectionResource<T> Create(
        IEnumerable<HalResource<T>> items,
        int pageNumber,
        int pageSize,
        int totalCount,
        Dictionary<string, HalLink> links)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new HalCollectionResource<T>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Links = links,
            Embedded = new HalCollectionEmbedded<T> { Items = items.ToList() }
        };
    }

    /// <summary>
    /// Creates a collection resource from pagination result.
    /// </summary>
    public static HalCollectionResource<T> FromPagination(
        IEnumerable<HalResource<T>> items,
        int pageNumber,
        int pageSize,
        int totalCount,
        int totalPages,
        Dictionary<string, HalLink> links)
    {
        return new HalCollectionResource<T>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Links = links,
            Embedded = new HalCollectionEmbedded<T> { Items = items.ToList() }
        };
    }
}

/// <summary>
/// Embedded container for collection items.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public sealed class HalCollectionEmbedded<T> where T : class
{
    /// <summary>
    /// The collection items, each wrapped as a HAL resource with its own links.
    /// </summary>
    [JsonPropertyName("items")]
    public List<HalResource<T>> Items { get; init; } = new();
}
