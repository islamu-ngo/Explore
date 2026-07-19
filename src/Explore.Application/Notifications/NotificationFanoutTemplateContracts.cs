// ABOUTME: Strict version-one JSON contracts for immutable event and session fanout occurrences.
// ABOUTME: Rejects unknown members so queued template payloads fail closed on contract drift.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Explore.Application.Notifications;

[JsonConverter(typeof(JsonStringEnumConverter<NotificationFanoutChangeField>))]
public enum NotificationFanoutChangeField
{
    Cancelled = 1,
    StartTime = 3,
    EndTime = 4,
    Timezone = 5,
    Location = 6,
    Room = 7
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NotificationFanoutChangeSetV1(
    NotificationFanoutChangeField[] Fields);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NotificationFanoutSnapshotV1(
    string EventTitle,
    string? SessionTitle,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Timezone,
    NotificationFanoutLocationSnapshotV1? Location);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NotificationFanoutLocationSnapshotV1(
    Guid EventLocationId,
    Guid? RoomId,
    string? Country,
    string? City,
    string? VenueName,
    string? RoomName,
    string? StreetAddress,
    string? Postcode);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(NotificationFanoutChangeSetV1))]
[JsonSerializable(typeof(NotificationFanoutSnapshotV1))]
[JsonSerializable(typeof(NotificationFanoutLocationSnapshotV1))]
public sealed partial class NotificationFanoutTemplateJsonContext : JsonSerializerContext;

public static class NotificationFanoutTemplateJson
{
    public static string Serialize(NotificationFanoutChangeSetV1 value) =>
        JsonSerializer.Serialize(value, NotificationFanoutTemplateJsonContext.Default.NotificationFanoutChangeSetV1);

    public static string Serialize(NotificationFanoutSnapshotV1 value) =>
        JsonSerializer.Serialize(value, NotificationFanoutTemplateJsonContext.Default.NotificationFanoutSnapshotV1);
}
