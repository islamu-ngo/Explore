// ABOUTME: Partial class extensions for NSwag-generated DTOs.
// ABOUTME: Adds properties missing from the generated schema that exist on backend DTOs.

namespace Explore.Blazor.Client.Clients;

public partial class EventDto
{
    [System.Text.Json.Serialization.JsonPropertyName("eventSeriesId")]
    public System.Guid? EventSeriesId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("seriesOrder")]
    public int? SeriesOrder { get; set; }
}

public partial class CreateEventSessionDto
{
    [System.Text.Json.Serialization.JsonPropertyName("featuredImageId")]
    public System.Guid? FeaturedImageId { get; set; }
}

public partial class UpdateEventSessionDto
{
    [System.Text.Json.Serialization.JsonPropertyName("featuredImageId")]
    public System.Guid? FeaturedImageId { get; set; }
}

public partial class EventSessionListDto
{
    [System.Text.Json.Serialization.JsonPropertyName("featuredImageId")]
    public System.Guid? FeaturedImageId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("featuredImageUri")]
    public string? FeaturedImageUri { get; set; }
}

public partial class CreateEventDto
{
}
