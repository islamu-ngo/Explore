// ABOUTME: Exposes evaluated feature flags for the authenticated user as a boolean dictionary.
// ABOUTME: Blazor UI calls this endpoint to hydrate its local FeatureStateContainer — no SDK in UI.

using Asp.Versioning;
using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/features")]
[ApiController]
[Authorize]
public class FeaturesController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;

    public FeaturesController(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService;
    }

    [HttpGet("my-flags")]
    public async Task<ActionResult<Dictionary<string, bool>>> GetMyFlags(CancellationToken ct)
    {
        var flags = await _featureFlagService.GetClientFlagsAsync(ct: ct);
        return Ok(flags);
    }
}
