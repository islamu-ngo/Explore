// ABOUTME: Thin API controller for configuring event participation.
// ABOUTME: Dispatches the existing CQRS command and maps command failures to RFC 7807 responses.

using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.EventParticipation.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/events/{eventId:guid}/participation")]
[ApiController]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public sealed class EventParticipationController : ControllerBase
{
    private static readonly ApiValidationProblemDescriptor ConfigureValidationProblem = new(
        "eventParticipation",
        "Event participation validation failed",
        "Event participation configuration failed.");

    private static readonly ApiNotFoundProblemDescriptor EventParticipationNotFoundProblem = new(
        "Event participation configuration not found",
        "Event participation configuration was not found.");

    private readonly IMediator _mediator;

    public EventParticipationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [HttpPatch("", Name = RouteNames.ConfigureEventParticipation)]
    [EndpointSummary("Configure event participation")]
    [EndpointDescription("Configures event participation for the event using the supplied concurrency stamp.")]
    [Consumes(HateoasConstants.JsonMediaType)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> Configure(
        Guid eventId,
        [FromBody] ConfigureEventParticipationDto participationConfiguration,
        [FromHeader(Name = "If-Match"), Required] string? ifMatch,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseConcurrencyStamp(ifMatch, out var expectedConcurrencyStamp))
        {
            return this.ToValidationProblem(
                ConfigureValidationProblem,
                "If-Match header is required and must contain the current event participation concurrency stamp.");
        }

        var response = await _mediator.Send(new ConfigureEventParticipationCommand
        {
            EventId = eventId,
            ExpectedConcurrencyStamp = expectedConcurrencyStamp,
            ParticipationConfiguration = participationConfiguration
        }, cancellationToken);

        if (!response.Success)
        {
            return response.FailureCode switch
            {
                "event_participation_configuration_not_found" =>
                    this.ToNotFoundProblem(EventParticipationNotFoundProblem, response.Message),
                "event_participation_configuration_concurrency_conflict" =>
                    this.ToCommandConflictProblem(
                        response,
                        "Event participation configuration conflict",
                        "Event participation configuration conflict."),
                _ => this.ToCommandValidationProblem(response, ConfigureValidationProblem)
            };
        }

        return Ok(response);
    }

    private static bool TryParseConcurrencyStamp(string? ifMatch, out Guid concurrencyStamp)
    {
        concurrencyStamp = default;
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return false;
        }

        var value = ifMatch.Trim();
        if (value.Length != 38 || value[0] != '"' || value[^1] != '"')
        {
            return false;
        }

        return Guid.TryParse(value[1..^1], out concurrencyStamp) && concurrencyStamp != Guid.Empty;
    }
}
