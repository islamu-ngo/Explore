// ABOUTME: Admin API controller for the Rule 12 custom-property governance report.
// ABOUTME: Surfaces promotion recommendations via Atlassian 4-question matrix for Layer 3 definitions.

using Asp.Versioning;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.Application.DTOs.CustomPropertyGovernance;
using Explore.Application.Features.CustomPropertyGovernance.Requests.Queries;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/admin/custom-property-definitions")]
[ApiController]
[Authorize]
public class CustomPropertyGovernanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomPropertyGovernanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get governance report listing all active Layer 3 definitions with promotion recommendations.
    /// </summary>
    [HttpGet("governance-report", Name = RouteNames.GetCustomPropertyGovernanceReport)]
    [EnableRateLimiting("authenticated")]
    [RequestTimeout(RequestTimeoutExtensions.LookupPolicy)]
    [ProducesResponseType(typeof(PaginatedResult<CustomPropertyGovernanceRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<CustomPropertyGovernanceRowDto>>> GetGovernanceReport(
        [FromQuery] Guid tenantId,
        [FromQuery] string? scope = null,
        [FromQuery] PromotionRecommendation? recommendation = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetCustomPropertyGovernanceReportQuery
            {
                TenantId = tenantId,
                Filter = new GovernanceReportFilterDto
                {
                    EntityScope = scope,
                    Recommendation = recommendation,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                }
            },
            cancellationToken);

        return Ok(result);
    }
}
