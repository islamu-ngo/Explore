// ABOUTME: Authenticated API surface for AI assistant bootstrap and future conversation workflows.
// ABOUTME: Exposes safe HAL bootstrap metadata while keeping provider secrets and history private.

using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.Hateoas;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Ai;
using Explore.Application.Features.AiAssistant.Requests.Commands;
using Explore.Application.Features.AiAssistant.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/ai/assistant")]
[ApiController]
[Authorize]
[EndpointClassification(EndpointClass.Authenticated)]
[Produces(HateoasConstants.JsonMediaType, HateoasConstants.HalJsonMediaType)]
public sealed class AiAssistantController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly IMediator _mediator;
    private readonly IHateoasLinkGenerator _linkGenerator;
    private readonly IResourceAssembler<AiConversationDto, AiConversationSummaryDto> _conversationAssembler;

    public AiAssistantController(
        IMediator mediator,
        IHateoasLinkGenerator linkGenerator,
        IResourceAssembler<AiConversationDto, AiConversationSummaryDto> conversationAssembler)
    {
        _mediator = mediator;
        _linkGenerator = linkGenerator;
        _conversationAssembler = conversationAssembler;
    }

    [HttpGet("bootstrap", Name = RouteNames.GetAiAssistantBootstrap)]
    [EndpointSummary("Get AI assistant bootstrap")]
    [EndpointDescription("Returns authenticated AI assistant availability, model choices, feature flags, limits, and HAL links without exposing provider secrets.")]
    [ProducesResponseType(typeof(HalResource<AiAssistantBootstrapDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalResource<AiAssistantBootstrapDto>>> GetBootstrap(CancellationToken cancellationToken = default)
    {
        var bootstrap = await _mediator.Send(new GetAiAssistantBootstrapQuery(), cancellationToken);
        var resource = new HalResource<AiAssistantBootstrapDto>(bootstrap);
        var selfPath = _linkGenerator.GeneratePath(RouteNames.GetAiAssistantBootstrap, null, HttpContext);

        if (!string.IsNullOrWhiteSpace(selfPath))
        {
            resource.WithLink(LinkRelations.Self, HalLink.Create(selfPath));
        }

        return Ok(resource);
    }

    [HttpGet("conversations", Name = RouteNames.GetAiConversations)]
    [EndpointSummary("Get AI conversations")]
    [EndpointDescription("Returns the authenticated user's recent private AI assistant conversations with HAL navigation links.")]
    [ProducesResponseType(typeof(HalCollectionResource<AiConversationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalCollectionResource<AiConversationSummaryDto>>> GetConversations(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var conversations = await _mediator.Send(new GetAiConversationListQuery { Limit = limit }, cancellationToken);
        var resource = await _conversationAssembler.ToCollectionResource(
            conversations,
            RouteNames.GetAiConversations,
            new { limit },
            HttpContext);

        return Ok(resource);
    }

    [HttpPost("conversations", Name = RouteNames.CreateAiConversation)]
    [EndpointSummary("Create AI conversation")]
    [EndpointDescription("Creates a private AI assistant conversation after tenant governance checks pass. The endpoint does not call a provider.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> CreateConversation(
        [FromBody] CreateAiConversationRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new CreateAiConversationCommand { Conversation = dto }, cancellationToken);

        if (!response.Success)
        {
            return this.ToAiAssistantProblem(response);
        }

        return CreatedAtRoute(RouteNames.GetAiConversation, new { conversationId = response.Id }, response);
    }

    [HttpGet("conversations/{conversationId:guid}", Name = RouteNames.GetAiConversation)]
    [EndpointSummary("Get AI conversation detail")]
    [EndpointDescription("Returns owned AI assistant conversation history with safe message, run, reference, and proposed-action metadata.")]
    [ProducesResponseType(typeof(HalResource<AiConversationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<AiConversationDto>>> GetConversation(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _mediator.Send(new GetAiConversationDetailQuery { ConversationId = conversationId }, cancellationToken);

        if (conversation is null)
        {
            return NotFound();
        }

        return Ok(await _conversationAssembler.ToResource(conversation, HttpContext));
    }

    [HttpPost("conversations/{conversationId:guid}/messages", Name = RouteNames.SendAiMessage)]
    [EndpointSummary("Send AI conversation message")]
    [EndpointDescription("Sends a bounded user message into an owned AI conversation using an Idempotency-Key header and guarded Application orchestration.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> SendMessage(
        Guid conversationId,
        [FromBody] SendAiMessageRequestDto dto,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            dto.IdempotencyKey = idempotencyKey;
        }

        var response = await _mediator.Send(new SendAiMessageCommand
        {
            ConversationId = conversationId,
            Message = dto
        }, cancellationToken);

        if (!response.Success)
        {
            return this.ToAiAssistantProblem(response);
        }

        return AcceptedAtRoute(RouteNames.GetAiRunStatus, new { conversationId, runId = response.Id }, response);
    }

    [HttpGet("conversations/{conversationId:guid}/runs/{runId:guid}", Name = RouteNames.GetAiRunStatus)]
    [EndpointSummary("Get AI run status")]
    [EndpointDescription("Returns safe status metadata for one AI provider run in an owned conversation.")]
    [ProducesResponseType(typeof(HalResource<AiRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<AiRunDto>>> GetRunStatus(
        Guid conversationId,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var run = await _mediator.Send(new GetAiRunStatusQuery
        {
            ConversationId = conversationId,
            RunId = runId
        }, cancellationToken);

        if (run is null)
        {
            return NotFound();
        }

        var resource = new HalResource<AiRunDto>(run);
        AddResourceLink(resource, LinkRelations.Self, RouteNames.GetAiRunStatus, new { conversationId, runId });
        AddResourceLink(resource, LinkRelations.Up, RouteNames.GetAiConversation, new { conversationId });
        return Ok(resource);
    }

    private void AddResourceLink<T>(
        HalResource<T> resource,
        string rel,
        string routeName,
        object? routeValues,
        string? method = null)
        where T : class
    {
        var path = _linkGenerator.GeneratePath(routeName, routeValues, HttpContext);

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        resource.WithLink(rel, method is null ? HalLink.Create(path) : HalLink.CreateAction(path, method));
    }

}
