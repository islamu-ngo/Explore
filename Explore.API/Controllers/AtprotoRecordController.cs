// ABOUTME: API controller for ATProto record management and federation operations.
// ABOUTME: Handles creation, updates, and deletion of ATProto records for federation support.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Features.AtprotoRecords.Requests.Commands;
using Explore.Application.Features.AtprotoRecords.Requests.Queries;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Authenticated)]
public class AtprotoRecordController : ControllerBase
{
    private const string ValidationFailedCode = "validation_failed";

    private readonly IMediator _mediator;
    private readonly ILogger<AtprotoRecordController> _logger;

    public AtprotoRecordController(
        IMediator mediator,
        ILogger<AtprotoRecordController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // GET: api/atprotoRecord
    [HttpGet(Name = RouteNames.GetAtprotoRecordEntries)]
    [Authorize]
    [OutputCache(PolicyName = "ListData")]
    public async Task<ActionResult<List<AtprotoRecordListDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var atprotoRecords = await _mediator.Send(new GetAtprotoRecordListRequest(), cancellationToken);
        return Ok(atprotoRecords);
    }

    // GET: api/atprotoRecord/{id}
    [HttpGet("{id}", Name = RouteNames.GetAtprotoRecordEntryById)]
    [Authorize]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<AtprotoRecordDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var atprotoRecord = await _mediator.Send(new GetAtprotoRecordDetailsRequest { Id = id }, cancellationToken);

        return Ok(atprotoRecord);
    }

    // POST: api/atprotoRecord
    [HttpPost(Name = RouteNames.CreateAtprotoRecordEntry)]
    [Authorize]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Create([FromBody] CreateAtprotoRecordDto dto, CancellationToken cancellationToken = default)
    {
        var command = new CreateAtprotoRecordCommand { AtprotoRecordDto = dto };
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    // PUT: api/atprotoRecord/{id}
    [HttpPut("{id}", Name = RouteNames.UpdateAtprotoRecordEntry)]
    [Authorize]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Update(Guid id, [FromBody] UpdateAtprotoRecordDto dto, CancellationToken cancellationToken = default)
    {
        if (id != dto.Id)
        {
            return ToValidationProblem(
                new BaseCommandResponse<Guid>
                {
                    Success = false,
                    Message = "AT Protocol record ID mismatch.",
                    FailureCode = ValidationFailedCode,
                    Errors = ["AT Protocol record ID mismatch."]
                },
                "AT Protocol record ID mismatch.");
        }

        var command = new UpdateAtprotoRecordCommand { AtprotoRecordDto = dto };
        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return ToValidationProblem(response, "AT Protocol record update failed.");
        }

        return Ok(response);
    }

    // DELETE: api/atprotoRecord/{id}
    [HttpDelete("{id}", Name = RouteNames.DeleteAtprotoRecordEntry)]
    [Authorize]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteAtprotoRecordCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    private ActionResult ToValidationProblem<TKey>(BaseCommandResponse<TKey> response, string fallbackDetail)
    {
        var errors = response.Errors is { Count: > 0 }
            ? response.Errors.ToArray()
            : [response.Message ?? fallbackDetail];

        var problemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["atprotoRecord"] = errors
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "AT Protocol record validation failed",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Detail = response.Message ?? fallbackDetail,
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["code"] = string.IsNullOrWhiteSpace(response.FailureCode)
            ? ValidationFailedCode
            : response.FailureCode;
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        if (HttpContext.Items["CorrelationId"] is string correlationId)
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" }
        };
    }
}
