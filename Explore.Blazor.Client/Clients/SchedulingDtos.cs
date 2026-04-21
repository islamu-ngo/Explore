// ABOUTME: Client-side DTO classes for scheduling entities (EventDay, EventAgendaItem, LocationRoom).
// ABOUTME: These mirror API response shapes; NSwag failed to generate standalone types for these entities.

using System.Text.Json.Serialization;

namespace Explore.Blazor.Client.Clients;

public class EventDayListDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("localDate")]
    public DateOnly LocalDate { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("isPublished")]
    public bool IsPublished { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("allowsDayScopeRegistration")]
    public bool AllowsDayScopeRegistration { get; set; }
}

public class EventDayDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("eventTitle")]
    public string? EventTitle { get; set; }

    [JsonPropertyName("localDate")]
    public DateOnly LocalDate { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("bannerText")]
    public string? BannerText { get; set; }

    [JsonPropertyName("bannerImageId")]
    public Guid? BannerImageId { get; set; }

    [JsonPropertyName("isPublished")]
    public bool IsPublished { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("allowsDayScopeRegistration")]
    public bool AllowsDayScopeRegistration { get; set; }

    [JsonPropertyName("tenantId")]
    public Guid TenantId { get; set; }
}

public class EventAgendaItemListDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTimeOffset EndTime { get; set; }

    [JsonPropertyName("localStartDate")]
    public DateOnly LocalStartDate { get; set; }

    [JsonPropertyName("localStartTime")]
    public TimeOnly LocalStartTime { get; set; }

    [JsonPropertyName("localEndTime")]
    public TimeOnly LocalEndTime { get; set; }

    [JsonPropertyName("kindId")]
    public int? KindId { get; set; }

    [JsonPropertyName("kindFullName")]
    public string? KindFullName { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
}

public class EventAgendaItemDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("eventTitle")]
    public string? EventTitle { get; set; }

    [JsonPropertyName("eventDayId")]
    public Guid? EventDayId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTimeOffset EndTime { get; set; }

    [JsonPropertyName("localStartDate")]
    public DateOnly LocalStartDate { get; set; }

    [JsonPropertyName("localEndDate")]
    public DateOnly LocalEndDate { get; set; }

    [JsonPropertyName("localStartTime")]
    public TimeOnly LocalStartTime { get; set; }

    [JsonPropertyName("localEndTime")]
    public TimeOnly LocalEndTime { get; set; }

    [JsonPropertyName("localStartMinuteOfDay")]
    public int LocalStartMinuteOfDay { get; set; }

    [JsonPropertyName("localEndMinuteOfDay")]
    public int LocalEndMinuteOfDay { get; set; }

    [JsonPropertyName("locationId")]
    public Guid? LocationId { get; set; }

    [JsonPropertyName("roomId")]
    public Guid? RoomId { get; set; }

    [JsonPropertyName("kindId")]
    public int? KindId { get; set; }

    [JsonPropertyName("kindFullName")]
    public string? KindFullName { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("tenantId")]
    public Guid TenantId { get; set; }
}

public class LocationRoomListDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("locationId")]
    public Guid LocationId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("capacity")]
    public int? Capacity { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
}

public class LocationRoomDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("locationId")]
    public Guid LocationId { get; set; }

    [JsonPropertyName("locationFullName")]
    public string? LocationFullName { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("capacity")]
    public int? Capacity { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("tenantId")]
    public Guid TenantId { get; set; }
}
