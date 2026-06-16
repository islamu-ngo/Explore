// ABOUTME: Management endpoints for persisted external API keys.
// ABOUTME: Keeps controllers thin by delegating issuance, listing, and revocation to MediatR handlers.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Hateoas;
using Explore.Application.DTOs.ExternalApiKey;
using Explore.Application.Features.ExternalApiKeys.Requests.Commands;
using Explore.Application.Features.ExternalApiKeys.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
public class ExternalApiKeyController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor CreateValidationProblem = new(
        "externalApiKey",
        "External API key validation failed",
        "External API key creation failed.");

    private static readonly ApiValidationProblemDescriptor UpdateValidationProblem = new(
        "externalApiKey",
        "External API key validation failed",
        "External API key update failed.");

    private static readonly ApiNotFoundProblemDescriptor ExternalApiKeyNotFoundProblem = new(
        "External API key not found",
        "External API key not found.");

    private readonly IMediator _mediator;

    public ExternalApiKeyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet(Name = RouteNames.GetExternalApiKeys)]
    [EndpointSummary("Get visible external API keys")]
    [EndpointDescription("Retrieve API keys owned by the current user or organizations they can manage.")]
    [ProducesResponseType(typeof(List<ExternalApiKeyListDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<ExternalApiKeyListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var keys = await _mediator.Send(new GetExternalApiKeyListRequest(), cancellationToken);
        return Ok(keys);
    }

    [HttpGet("{id}", Name = RouteNames.GetExternalApiKeyById)]
    [EndpointSummary("Get external API key details")]
    [EndpointDescription("Retrieve metadata for a specific external API key visible to the current caller.")]
    [ProducesResponseType(typeof(ExternalApiKeyListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<ExternalApiKeyListDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var key = await _mediator.Send(new GetExternalApiKeyDetailsRequest { Id = id }, cancellationToken);
        if (key == null)
            return this.ToNotFoundProblem(ExternalApiKeyNotFoundProblem);

        return Ok(key);
    }

    [HttpPost(Name = RouteNames.CreateExternalApiKey)]
    [EndpointSummary("Create a new external API key")]
    [EndpointDescription("Issue a tenant-bound external API key and reveal the raw secret once.")]
    [ProducesResponseType(typeof(CreateExternalApiKeyCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateExternalApiKeyCommandResponse>> Create([FromBody] CreateExternalApiKeyDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateExternalApiKeyCommand { ExternalApiKeyDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return this.ToCommandValidationProblem(response, CreateValidationProblem);
        }

        return Ok(response);
    }

    [HttpPut("{id}", Name = RouteNames.UpdateExternalApiKey)]
    [EndpointSummary("Update an external API key policy")]
    [EndpointDescription("Update editable policy fields for a visible external API key.")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateExternalApiKeyPolicyDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return this.ToValidationProblem(UpdateValidationProblem, "External API key ID mismatch.");
        }

        var command = new UpdateExternalApiKeyPolicyCommand { ExternalApiKeyPolicyDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            if (response.Message == "External API key not found.")
            {
                return this.ToNotFoundProblem(ExternalApiKeyNotFoundProblem);
            }

            return this.ToCommandValidationProblem(response, UpdateValidationProblem);
        }

        return Ok(response);
    }

    [HttpDelete("{id}", Name = RouteNames.DeleteExternalApiKey)]
    [EndpointSummary("Revoke an external API key")]
    [EndpointDescription("Revoke an external API key visible to the current caller.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new RevokeExternalApiKeyCommand { Id = id }, cancellationToken);

        return NoContent();
    }

    [HttpGet("usage-report", Name = RouteNames.GetExternalApiKeyUsageReport)]
    [EndpointSummary("Get API key usage report")]
    [EndpointDescription("Aggregated request counts and credit usage per API key. Instance admins see platform-wide data; tenant admins see their tenant only. Does not expose secret material.")]
    [ProducesResponseType(typeof(List<ExternalApiKeyUsageReportDto>), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<ExternalApiKeyUsageReportDto>>> GetUsageReport(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var report = await _mediator.Send(new GetExternalApiKeyUsageReportRequest
        {
            From = from,
            To = to,
            TenantId = tenantId
        }, cancellationToken);

        return Ok(report);
    }
}
