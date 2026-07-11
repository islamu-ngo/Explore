namespace Explore.Application.Hateoas;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// JSON converter factory for HalResource&lt;T&gt; that creates type-specific converters.
/// </summary>
public sealed class HalResourceJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        return typeToConvert.GetGenericTypeDefinition() == typeof(HalResource<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var dataType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(HalResourceJsonConverter<>).MakeGenericType(dataType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// JSON converter for HalResource&lt;T&gt; that flattens the Data property to root level.
/// This ensures HAL-compliant JSON output where resource properties appear at the root
/// alongside _links and _embedded.
/// </summary>
/// <typeparam name="T">The type of the resource data.</typeparam>
public sealed class HalResourceJsonConverter<T> : JsonConverter<HalResource<T>> where T : class
{
    public override HalResource<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        // Extract _links
        var links = new Dictionary<string, HalLink>();
        if (root.TryGetProperty("_links", out var linksElement))
        {
            links = JsonSerializer.Deserialize<Dictionary<string, HalLink>>(linksElement.GetRawText(), options)
                ?? new Dictionary<string, HalLink>();
        }

        // Extract _embedded
        Dictionary<string, object>? embedded = null;
        if (root.TryGetProperty("_embedded", out var embeddedElement))
        {
            embedded = JsonSerializer.Deserialize<Dictionary<string, object>>(embeddedElement.GetRawText(), options);
        }

        // Create a new JSON object without _links and _embedded for data deserialization
        var dataProperties = new Dictionary<string, JsonElement>();
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name != "_links" && property.Name != "_embedded")
            {
                dataProperties[property.Name] = property.Value;
            }
        }

        var dataJson = JsonSerializer.Serialize(dataProperties, options);
        var data = JsonSerializer.Deserialize<T>(dataJson, options);

        return new HalResource<T>
        {
            Data = data!,
            Links = links,
            Embedded = embedded
        };
    }

    public override void Write(Utf8JsonWriter writer, HalResource<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write the data properties at root level (flattened)
        if (value.Data is not null)
        {
            var dataJson = JsonSerializer.SerializeToElement(value.Data, options);
            foreach (var property in dataJson.EnumerateObject())
            {
                property.WriteTo(writer);
            }
        }

        // Write _links only if present
        if (value.Links is not null && value.Links.Count > 0)
        {
            writer.WritePropertyName("_links");
            JsonSerializer.Serialize(writer, value.Links, options);
        }

        // Write _embedded only if present
        if (value.Embedded is not null && value.Embedded.Count > 0)
        {
            writer.WritePropertyName("_embedded");
            JsonSerializer.Serialize(writer, value.Embedded, options);
        }

        writer.WriteEndObject();
    }
}
