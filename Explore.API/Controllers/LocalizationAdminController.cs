// ABOUTME: Admin API controller for managing TMS (Translation Management System) configuration.
// ABOUTME: Provides endpoints to test TMS connection, view config, and trigger translation exports.

using Asp.Versioning;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Localization;
using Explore.Application.Features.Localization.Requests.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/admin/localization")]
[ApiController]
[Authorize]
public class LocalizationAdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITranslationConfigResolver _configResolver;

    public LocalizationAdminController(IMediator mediator, ITranslationConfigResolver configResolver)
    {
        _mediator = mediator;
        _configResolver = configResolver;
    }

    /// <summary>
    /// Test connection to the configured TMS provider.
    /// </summary>
    [HttpPost("test-connection")]
    [EndpointSummary("Test TMS Connection")]
    [EndpointDescription("Verifies that the configured Translation Management System (Tolgee/Weblate) is reachable.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> TestConnection(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new TestTmsConnectionCommand(), cancellationToken);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }

    /// <summary>
    /// Get current localization configuration.
    /// </summary>
    [HttpGet("configuration")]
    [EndpointSummary("Get Localization Configuration")]
    [EndpointDescription("Returns the current TMS provider settings and connection status.")]
    [ProducesResponseType(typeof(LocalizationConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LocalizationConfigDto>> GetConfiguration(CancellationToken cancellationToken = default)
    {
        var config = await _configResolver.ResolveAsync(cancellationToken);

        var dto = new LocalizationConfigDto
        {
            DefaultLanguage = config.DefaultLanguage,
            TmsProvider = config.Provider.ToString(),
            TmsApiUrl = config.ApiUrl,
            TmsProjectId = config.ProjectId,
            TmsComponent = config.Component,
        };

        return Ok(dto);
    }

    /// <summary>
    /// Export translations from TMS for a specific language.
    /// </summary>
    [HttpPost("export-from-tms")]
    [EndpointSummary("Export Translations from TMS")]
    [EndpointDescription("Pulls translations from the connected TMS for the specified language and refreshes the cache.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ExportFromTms(
        [FromQuery] string languageCode,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ExportFromTmsCommand { LanguageCode = languageCode },
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }
}
