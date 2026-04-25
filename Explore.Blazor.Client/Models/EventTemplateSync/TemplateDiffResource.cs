namespace Explore.Blazor.Client.Models.EventTemplateSync;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class TemplateDiffResource
{
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLinkDto> Links { get; set; } = new();

    public bool HasHalLink(string rel) => Links != null && Links.ContainsKey(rel);
}

public class HalLinkDto
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = "";

    [JsonPropertyName("method")]
    public string Method { get; set; } = "GET";
}
