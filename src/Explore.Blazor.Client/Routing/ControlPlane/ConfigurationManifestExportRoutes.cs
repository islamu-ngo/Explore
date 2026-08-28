// ABOUTME: Defines the fixed same-origin BFF route used for configuration-manifest downloads.
// ABOUTME: Keeps browser code independent from privileged API routes and deployment host topology.

namespace Explore.Blazor.Client.Routing.ControlPlane;

public static class ConfigurationManifestExportRoutes
{
    public const string BffExport = "/bff/control-plane/configuration-manifest/export";
}
