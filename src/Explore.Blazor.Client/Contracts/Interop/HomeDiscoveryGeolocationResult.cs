// ABOUTME: Transient browser-geolocation result returned only to the public home-discovery component.
// ABOUTME: Represents available, denied, and unavailable outcomes without persisting precise coordinates.

using System.Text.Json.Serialization;

namespace Explore.Blazor.Client.Contracts.Interop;

public sealed record HomeDiscoveryGeolocationResult(
    HomeDiscoveryGeolocationStatus Status,
    double? Latitude = null,
    double? Longitude = null);

[JsonConverter(typeof(JsonStringEnumConverter<HomeDiscoveryGeolocationStatus>))]
public enum HomeDiscoveryGeolocationStatus
{
    Available,
    Denied,
    Unavailable
}
