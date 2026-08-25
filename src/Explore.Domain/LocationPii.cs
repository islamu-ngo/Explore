// ABOUTME: Stores precise location-identifying fields in a dedicated extension table.
// ABOUTME: Uses a 1:1 shared primary-key relationship with Location for hard-deleteable PII.

using Explore.Domain.ValueObjects;

namespace Explore.Domain;

public class LocationPii
{
    private const string RedactedDisplay = "LocationPii[redacted]";

    public Guid LocationId { get; set; }
    public Location? Location { get; set; }

    public required string Address { get; set; }
    public required string Postcode { get; set; }

    // Nullable scalar columns remain the Phase 10 persistence shape. Domain writes pair them
    // through GeoCoordinate so no mutation boundary can expose a half-written coordinate.
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public GeoCoordinate? GetCoordinate() => HasValidCoordinatePair()
        ? GeoCoordinate.Create(Latitude!.Value, Longitude!.Value)
        : null;

    public static LocationPii Create(
        string address,
        string postcode,
        GeoCoordinate? coordinate)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(postcode);

        var pii = new LocationPii
        {
            Address = address,
            Postcode = postcode
        };
        pii.SetAddress(address, postcode, coordinate);
        return pii;
    }

    internal void SetAddress(
        string address,
        string postcode,
        GeoCoordinate? coordinate)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(postcode);

        Address = address;
        Postcode = postcode;
        Latitude = coordinate?.Latitude;
        Longitude = coordinate?.Longitude;
    }

    public override string ToString() => RedactedDisplay;

    private bool HasValidCoordinatePair() =>
        Latitude is { } latitude
        && Longitude is { } longitude
        && double.IsFinite(latitude)
        && double.IsFinite(longitude)
        && latitude is >= -90 and <= 90
        && longitude is >= -180 and <= 180;
}
