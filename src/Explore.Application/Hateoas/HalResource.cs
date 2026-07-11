namespace Explore.Application.Hateoas;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a HAL (Hypertext Application Language) resource.
/// The resource data is flattened to the root level alongside _links and _embedded.
/// Conforms to the HAL specification: https://stateless.group/hal_specification.html
/// </summary>
/// <typeparam name="T">The type of the resource data (DTO).</typeparam>
[JsonConverter(typeof(HalResourceJsonConverterFactory))]
public class HalResource<T> where T : class
{
    /// <summary>
    /// The resource data. Properties are flattened to root level during serialization.
    /// </summary>
    public T Data { get; init; } = default!;

    /// <summary>
    /// HAL links for this resource.
    /// Key is the link relation (e.g., "self", "collection", "next").
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; init; } = new();

    /// <summary>
    /// Embedded resources.
    /// Key is the relation name (e.g., "sessions", "categories").
    /// </summary>
    [JsonPropertyName("_embedded")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Embedded { get; init; }

    /// <summary>
    /// Creates an empty HAL resource.
    /// </summary>
    public HalResource() { }

    /// <summary>
    /// Creates a HAL resource with data.
    /// </summary>
    public HalResource(T data)
    {
        Data = data;
    }

    /// <summary>
    /// Creates a HAL resource with data and links.
    /// </summary>
    public HalResource(T data, Dictionary<string, HalLink> links)
    {
        Data = data;
        Links = links;
    }

    /// <summary>
    /// Adds a link to this resource.
    /// </summary>
    public HalResource<T> WithLink(string rel, HalLink link)
    {
        Links[rel] = link;
        return this;
    }

    /// <summary>
    /// Adds a self link to this resource.
    /// </summary>
    public HalResource<T> WithSelfLink(string href)
    {
        Links[LinkRelations.Self] = HalLink.Create(href);
        return this;
    }

    /// <summary>
    /// Adds embedded resources.
    /// </summary>
    public HalResource<T> WithEmbedded(string rel, object embedded)
    {
        var embeddedDict = Embedded ?? new Dictionary<string, object>();
        embeddedDict[rel] = embedded;
        return new HalResource<T>
        {
            Data = Data,
            Links = Links,
            Embedded = embeddedDict
        };
    }
}

/// <summary>
/// Non-generic base for HAL resources, used for embedded resources.
/// </summary>
public abstract class HalResourceBase
{
    /// <summary>
    /// HAL links for this resource.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; init; } = new();

    /// <summary>
    /// Embedded resources.
    /// </summary>
    [JsonPropertyName("_embedded")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Embedded { get; init; }
}
