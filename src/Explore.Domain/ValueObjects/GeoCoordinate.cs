// ABOUTME: Represents one validated finite latitude/longitude pair without spatial dependencies.
// ABOUTME: Redacts exact values from diagnostic formatting because coordinates are location PII.

namespace Explore.Domain.ValueObjects;

public sealed record GeoCoordinate
{
    private GeoCoordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }
    public double Longitude { get; }

    public static GeoCoordinate Create(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude));
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude));
        }

        return new GeoCoordinate(latitude, longitude);
    }

    public override string ToString() => "GeoCoordinate[redacted]";
}
