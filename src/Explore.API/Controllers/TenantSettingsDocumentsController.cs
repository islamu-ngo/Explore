// ABOUTME: Focused API controller for tenant typed settings document reads during settings cutover.
// ABOUTME: Keeps typed document endpoints separate from scalar settings endpoints.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.TenantSettingsDocuments;
using Explore.Application.Features.TenantSettingsDocuments.Requests.Commands;
using Explore.Application.Features.TenantSettingsDocuments.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/tenant/settings/documents")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
public sealed class TenantSettingsDocumentsController(
    IMediator mediator,
    ITenantContext tenantContext,
    ITenantBrandingSettingsDocumentLockService lockService,
    IResourceAssembler<TenantBrandingSettingsDocumentDto, TenantBrandingSettingsDocumentDto> resourceAssembler)
    : ExploreControllerBase
{
    private static readonly ApiValidationProblemDescriptor ReplaceBrandingValidationProblem = new(
        "tenantBrandingSettingsDocument",
        "Tenant branding settings document validation failed",
        "Tenant branding settings document replacement failed.");

    private static readonly ApiNotFoundProblemDescriptor BrandingDocumentNotFoundProblem = new(
        "Tenant branding settings document not found",
        "Tenant branding settings document not found.");

    [HttpGet("branding", Name = RouteNames.GetTenantBrandingSettingsDocument)]
    [EndpointSummary("Get Tenant Branding Settings Document")]
    [EndpointDescription("Returns the current tenant branding typed settings document, provisioning the default typed row when it is missing.")]
    [ProducesResponseType(typeof(HalResource<TenantBrandingSettingsDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<TenantBrandingSettingsDocumentDto>>> GetBranding(
        CancellationToken cancellationToken = default)
    {
        var document = await mediator.Send(new GetTenantBrandingSettingsDocumentQuery(), cancellationToken);

        if (document is null)
        {
            return this.ToNotFoundProblem(BrandingDocumentNotFoundProblem);
        }

        var resource = await resourceAssembler.ToResource(document, HttpContext);
        return Ok(resource);
    }

    [HttpPut("branding", Name = RouteNames.ReplaceTenantBrandingSettingsDocument)]
    [EndpointSummary("Replace Tenant Branding Settings Document")]
    [EndpointDescription("Fully replaces the current tenant branding typed settings document. Uses optimistic concurrency and writes only tenant JSONB settings documents.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(HalResource<TenantBrandingSettingsDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<TenantBrandingSettingsDocumentDto>>> ReplaceBranding(
        [FromBody] ReplaceTenantBrandingSettingsDocumentDto document,
        [FromServices] IOutputCacheStore cacheStore,
        CancellationToken cancellationToken = default)
    {
        var lockState = await lockService.GetLockStateAsync(cancellationToken);
        var response = await mediator.Send(
            new ReplaceTenantBrandingSettingsDocumentCommand
            {
                TenantId = tenantContext.TenantId,
                Document = document,
                IsLockedByInstance = lockState.IsLockedByInstance
            },
            cancellationToken);

        if (!response.Success)
        {
            if (response.Message == "Tenant branding settings document not found.")
            {
                return this.ToNotFoundProblem(BrandingDocumentNotFoundProblem);
            }

            return this.ToCommandValidationProblem(response, ReplaceBrandingValidationProblem);
        }

        await cacheStore.EvictByTagAsync("public-experience-shell", cancellationToken);

        var updated = await mediator.Send(new GetTenantBrandingSettingsDocumentQuery(), cancellationToken);
        if (updated is null)
        {
            return this.ToNotFoundProblem(BrandingDocumentNotFoundProblem);
        }

        var resource = await resourceAssembler.ToResource(updated, HttpContext);
        return Ok(resource);
    }
}
