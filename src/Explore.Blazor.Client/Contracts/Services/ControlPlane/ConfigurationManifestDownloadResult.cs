// ABOUTME: Immutable outcome of one HAL-gated whole-instance configuration-manifest download attempt.
// ABOUTME: Carries the revalidated capability resource so callers re-gate affordances without a second read.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.ControlPlane;

public sealed record ConfigurationManifestDownloadResult(
    bool Started,
    HalResourceOfControlPlaneOverviewDto Capabilities);
