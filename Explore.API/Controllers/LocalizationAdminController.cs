// ABOUTME: Admin API controller for managing TMS (Translation Management System) configuration.
// ABOUTME: Provides endpoints to test TMS connection, view config, export bundles, and health probes.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Localization;
using Explore.Application.Features.Localization.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/admin/localization")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
public class LocalizationAdminController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor TestConnectionValidationProblem = new(
        "localizationTmsConnection",
        "Localization TMS connection validation failed",
        "Localization TMS connection test failed.");

    private static readonly ApiValidationProblemDescriptor GovernanceValidationProblem = new(
        "localizationGovernance",
        "Localization governance validation failed",
        "Localization governance update failed.");

    private static readonly ApiValidationProblemDescriptor ExportValidationProblem = new(
        "localizationExport",
        "Localization export validation failed",
        "Localization export from TMS failed.");

    private readonly IMediator _mediator;
    private readonly ITranslationConfigResolver _configResolver;
    private readonly IBundleFileWriter _bundleFileWriter;

    public LocalizationAdminController(
        IMediator mediator,
        ITranslationConfigResolver configResolver,
        IBundleFileWriter bundleFileWriter)
    {
        _mediator = mediator;
        _configResolver = configResolver;
        _bundleFileWriter = bundleFileWriter;
    }

    /// <summary>
    /// Test connection to the configured TMS provider.
    /// </summary>
    [HttpPost("test-connection", Name = RouteNames.TestLocalizationTmsConnection)]
    [EndpointSummary("Test TMS Connection")]
    [EndpointDescription("Verifies that the configured Translation Management System (Tolgee/Weblate) is reachable.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> TestConnection(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new TestTmsConnectionCommand(), cancellationToken);

        if (result.Success)
            return Ok(result);

        return this.ToCommandValidationProblem(result, TestConnectionValidationProblem);
    }

    /// <summary>
    /// Get current localization configuration.
    /// </summary>
    [HttpGet("configuration", Name = RouteNames.GetLocalizationConfiguration)]
    [EndpointSummary("Get Localization Configuration")]
    [EndpointDescription("Returns the current TMS provider settings and connection status.")]
    [ProducesResponseType(typeof(LocalizationConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
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
            EnabledLanguages = config.EnabledLanguages.ToList(),
            FallbackLanguage = config.FallbackLanguage,
            ClientPickerEnabled = config.ClientPickerEnabled,
            ForceOfflineMode = config.ForceOfflineMode,
        };

        return Ok(dto);
    }

    /// <summary>
    /// Probes the writable bundle path for health — directory existence and write permission.
    /// </summary>
    [HttpGet("bundle-health", Name = RouteNames.CheckLocalizationBundleHealth)]
    [EndpointSummary("Check Bundle Path Health")]
    [EndpointDescription("Reports whether the offline bundle target directory is writable. Admin UI surfaces this as a health banner.")]
    [ProducesResponseType(typeof(WritablePathHealth), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WritablePathHealth>> GetBundlePathHealth(CancellationToken cancellationToken = default)
    {
        var health = await _bundleFileWriter.CheckHealthAsync(cancellationToken);
        return Ok(health);
    }

    /// <summary>
    /// Update localization governance settings (TMS provider, enabled languages, kill-switches, fallback language).
    /// </summary>
    [HttpPut("governance", Name = RouteNames.UpdateLocalizationGovernance)]
    [EndpointSummary("Update Localization Governance")]
    [EndpointDescription("Persists TMS provider configuration, enabled languages, fallback language, and kill-switches.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> UpdateGovernance(
        [FromBody] UpdateLocalizationGovernanceDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new UpdateLocalizationGovernanceCommand { Dto = dto },
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return this.ToCommandValidationProblem(result, GovernanceValidationProblem);
    }

    /// <summary>
    /// Export translations from TMS for a specific language.
    /// </summary>
    [HttpPost("export-from-tms", Name = RouteNames.ExportLocalizationFromTms)]
    [EndpointSummary("Export Translations from TMS")]
    [EndpointDescription("Pulls translations from the connected TMS for the specified language and refreshes the cache.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ExportFromTms(
        [FromQuery] string languageCode,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ExportFromTmsCommand { LanguageCode = languageCode },
            cancellationToken);

        if (result.Success)
            return Ok(result);

        return this.ToCommandValidationProblem(result, ExportValidationProblem);
    }
}
