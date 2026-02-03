namespace Explore.Application.Hateoas;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a HAL (Hypertext Application Language) link.
/// Conforms to the HAL specification: https://stateless.group/hal_specification.html
/// </summary>
public sealed class HalLink
{
    /// <summary>
    /// The URI of the linked resource.
    /// This is the only REQUIRED property.
    /// </summary>
    [JsonPropertyName("href")]
    public required string Href { get; init; }

    /// <summary>
    /// Indicates whether the href is a URI Template (RFC 6570).
    /// If true, clients should treat href as a template.
    /// </summary>
    [JsonPropertyName("templated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Templated { get; init; }

    /// <summary>
    /// The media type expected when dereferencing the target resource.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    /// <summary>
    /// Indicates the language of the target resource (BCP 47 language tag).
    /// </summary>
    [JsonPropertyName("hreflang")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hreflang { get; init; }

    /// <summary>
    /// Human-readable label for the link.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    /// <summary>
    /// A secondary key for selecting links with the same relation type.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>
    /// The HTTP method to use when following this link.
    /// Not part of HAL spec, but commonly used extension for action links.
    /// </summary>
    [JsonPropertyName("method")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Method { get; init; }

    /// <summary>
    /// Creates a simple link with just an href.
    /// </summary>
    public static HalLink Create(string href) => new() { Href = href };

    /// <summary>
    /// Creates an action link with href and HTTP method.
    /// </summary>
    public static HalLink CreateAction(string href, string method) =>
        new() { Href = href, Method = method };

    /// <summary>
    /// Creates a templated link.
    /// </summary>
    public static HalLink CreateTemplated(string hrefTemplate, string? title = null) =>
        new() { Href = hrefTemplate, Templated = true, Title = title };
}
