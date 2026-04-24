// ABOUTME: Client-side HAL link DTO mirror; deserializes the `_links[rel]` entries emitted by HateoasLinkGenerator.
// ABOUTME: Uses camelCase JSON property names to match the server serializer contract.

using System.Text.Json.Serialization;

namespace Explore.Blazor.Client.Models;

public sealed record HalLinkDto(
    [property: JsonPropertyName("href")] string Href,
    [property: JsonPropertyName("method")] string? Method = null,
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("templated")] bool? Templated = null);
