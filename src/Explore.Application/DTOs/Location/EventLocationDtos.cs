// ABOUTME: Constrained public, attendee, management, policy, and update EventLocation DTO contracts.
// ABOUTME: Materializes response shapes only from validated disclosure results to prevent contradictory states.

using System.Text.Json.Serialization;
using Explore.Application.Contracts.LocationPrivacy;

namespace Explore.Application.DTOs.Location;

public sealed record EventLocationPublicDto
{
    private EventLocationPublicDto(
        Guid eventLocationId,
        EventLocationDisclosureState state,
        EventLocationPublicFieldsDto? fields)
    {
        EventLocationId = eventLocationId;
        State = state;
        Fields = fields;
    }

    public Guid EventLocationId { get; }
    public EventLocationDisclosureState State { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EventLocationPublicFieldsDto? Fields { get; }

    public static EventLocationPublicDto FromDisclosureResult(EventLocationDisclosureResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Purpose != EventLocationDisclosurePurpose.Public)
        {
            throw new ArgumentException("Public DTO requires a public disclosure result.", nameof(result));
        }

        return new(result.EventLocationId, result.State, MapFields(result.Values));
    }

    private static EventLocationPublicFieldsDto? MapFields(EventLocationDisclosureValues? values)
        => values is null
            ? null
            : new(
                values.Country,
                values.Timezone,
                values.City,
                values.VenueName,
                values.RoomName,
                values.StreetAddress,
                values.Postcode,
                values.Latitude,
                values.Longitude,
                values.FormattedAddress,
                values.MapUrl,
                values.Geohash);
}

public sealed record EventLocationPublicFieldsDto(
    string? Country,
    string? Timezone,
    string? City,
    string? VenueName,
    string? RoomName,
    string? StreetAddress,
    string? Postcode,
    double? Latitude,
    double? Longitude,
    string? FormattedAddress,
    string? MapUrl,
    string? Geohash);

public sealed record EventLocationAttendeeDto
{
    private EventLocationAttendeeDto(
        Guid eventLocationId,
        EventLocationDisclosureState state,
        EventLocationAttendeeFieldsDto? fields)
    {
        EventLocationId = eventLocationId;
        State = state;
        Fields = fields;
    }

    public Guid EventLocationId { get; }
    public EventLocationDisclosureState State { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EventLocationAttendeeFieldsDto? Fields { get; }

    public static EventLocationAttendeeDto FromDisclosureResult(EventLocationDisclosureResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Purpose != EventLocationDisclosurePurpose.Attendee)
        {
            throw new ArgumentException("Attendee DTO requires an attendee disclosure result.", nameof(result));
        }

        return new(result.EventLocationId, result.State, MapFields(result.Values));
    }

    private static EventLocationAttendeeFieldsDto? MapFields(EventLocationDisclosureValues? values)
        => values is null
            ? null
            : new(
                values.Country,
                values.Timezone,
                values.City,
                values.VenueName,
                values.RoomName,
                values.StreetAddress,
                values.Postcode,
                values.Latitude,
                values.Longitude,
                values.FormattedAddress,
                values.MapUrl,
                values.Geohash);
}

public sealed record EventLocationAttendeeFieldsDto(
    string? Country,
    string? Timezone,
    string? City,
    string? VenueName,
    string? RoomName,
    string? StreetAddress,
    string? Postcode,
    double? Latitude,
    double? Longitude,
    string? FormattedAddress,
    string? MapUrl,
    string? Geohash);

public sealed record EventLocationManagementDto
{
    private EventLocationManagementDto(
        Guid eventLocationId,
        Guid? locationId,
        EventLocationDisclosureState state,
        EventLocationManagementFieldsDto? fields,
        EventLocationDisclosurePolicyDto policy,
        bool needsPrivacyReview,
        int policyVersion,
        Guid concurrencyStamp)
    {
        EventLocationId = eventLocationId;
        LocationId = locationId;
        State = state;
        Fields = fields;
        Policy = policy;
        NeedsPrivacyReview = needsPrivacyReview;
        PolicyVersion = policyVersion;
        ConcurrencyStamp = concurrencyStamp;
    }

    public Guid EventLocationId { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? LocationId { get; }

    public EventLocationDisclosureState State { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EventLocationManagementFieldsDto? Fields { get; }

    public EventLocationDisclosurePolicyDto Policy { get; }
    public bool NeedsPrivacyReview { get; }
    public int PolicyVersion { get; }
    public Guid ConcurrencyStamp { get; }

    public static EventLocationManagementDto FromDisclosureResult(
        EventLocationDisclosureResult result,
        EventLocationDisclosurePolicyDto policy,
        bool needsPrivacyReview,
        int policyVersion,
        Guid concurrencyStamp)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(policy);
        if (result.Purpose != EventLocationDisclosurePurpose.Management)
        {
            throw new ArgumentException("Management DTO requires a management disclosure result.", nameof(result));
        }

        return new(
            result.EventLocationId,
            result.LocationId,
            result.State,
            MapFields(result.Values),
            policy,
            needsPrivacyReview,
            policyVersion,
            concurrencyStamp);
    }

    private static EventLocationManagementFieldsDto? MapFields(EventLocationDisclosureValues? values)
        => values is null
            ? null
            : new(
                values.Country,
                values.Timezone,
                values.City,
                values.VenueName,
                values.RoomName,
                values.RoomDescription,
                values.StreetAddress,
                values.Postcode,
                values.Latitude,
                values.Longitude,
                values.FormattedAddress,
                values.MapUrl,
                values.Geohash);
}

public sealed record EventLocationManagementFieldsDto(
    string? Country,
    string? Timezone,
    string? City,
    string? VenueName,
    string? RoomName,
    string? RoomDescription,
    string? StreetAddress,
    string? Postcode,
    double? Latitude,
    double? Longitude,
    string? FormattedAddress,
    string? MapUrl,
    string? Geohash);

public sealed record EventLocationDisclosurePolicyDto(
    bool ShowVenueName,
    bool ShowCity,
    bool ShowCountry,
    bool ShowRoomName,
    bool ShowStreetAddress,
    bool ShowPostcode,
    bool ShowCoordinates,
    int FullDetailsAudienceId,
    DateTime? RevealFullDetailsFromUtc);

public sealed record UpdateEventLocationDisclosureDto(
    bool ShowVenueName,
    bool ShowCity,
    bool ShowCountry,
    bool ShowRoomName,
    bool ShowStreetAddress,
    bool ShowPostcode,
    bool ShowCoordinates,
    int FullDetailsAudienceId,
    DateTime? RevealFullDetailsFromUtc,
    int ExpectedPolicyVersion,
    Guid ExpectedConcurrencyStamp);
