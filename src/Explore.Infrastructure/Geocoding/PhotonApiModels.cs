// ABOUTME: Defines internal Photon response metadata that never crosses the Application boundary.
// ABOUTME: Keeps deployment-owned provenance separate from runtime provider configuration.

namespace Explore.Infrastructure.Geocoding;

internal static class PhotonProvenance
{
    public const string Provider = "Photon";
    public const string Attribution = "OpenStreetMap contributors (ODbL)";
}
