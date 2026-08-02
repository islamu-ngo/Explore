// ABOUTME: Exposes capability-scoped guest and current-account registration-order lifecycle endpoints.
// ABOUTME: Transports guest capabilities only in headers and delegates all order access decisions to MediatR.

using Asp.Versioning;
using System.Text.Json;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Extensions;
using Explore.API.Filters;
using Explore.API.Hateoas;
using Explore.API.Models;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.DTOs.RegistrationSubmissions;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Features.RegistrationOrders.Requests.Queries;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/events/{eventId:guid}/registration-orders")]
[ApiController]
public sealed class RegistrationOrderController(
    IMediator mediator,
    IResourceAssembler<RegistrationOrderDto, RegistrationOrderDto> assembler) : ControllerBase
{
    private const string CapabilityHeader = "X-Registration-Order-Capability";
    private const string AttemptCapabilityHeader = "X-Registration-Attempt-Capability";
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private static readonly ApiValidationProblemDescriptor RegistrationOrderValidationProblem = new(
        "registrationOrder",
        "Registration order request failed",
        "Registration order request failed.");

    private static readonly ApiNotFoundProblemDescriptor RegistrationOrderNotFoundProblem = new(
        "Registration order not found",
        "Registration order was not found.");

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [PrivateNoStore]
    [HttpGet("checkout", Name = RouteNames.GetRegistrationCheckoutComposition)]
    [EndpointSummary("Get registration checkout composition")]
    [EndpointDescription("Returns the current published ticket choices for a publicly eligible platform-managed event.")]
    [ProducesResponseType(typeof(RegistrationCheckoutCompositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationCheckoutCompositionDto>> GetCheckout(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetRegistrationCheckoutCompositionQuery(eventId), cancellationToken);
        return response is null ? this.ToNotFoundProblem(RegistrationOrderNotFoundProblem) : Ok(response);
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.PublicTransactional)]
    [EnableRateLimiting(RateLimitingExtensions.PublicTransactionalPolicy)]
    [RequireIdempotencyKey]
    [HttpPost("guest", Name = RouteNames.StartGuestRegistrationOrder)]
    [EndpointSummary("Start guest registration order")]
    [EndpointDescription("Creates an anonymous registration order and reveals its opaque recovery capability once in a response header.")]
    [Consumes(HateoasConstants.JsonMediaType)]
    [ProducesResponseType(typeof(GuestRegistrationOrderStartDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GuestRegistrationOrderStartDto>> StartGuest(
        Guid eventId,
        [FromBody] StartRegistrationOrderRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return this.ToValidationProblem(RegistrationOrderValidationProblem, "A registration-order payload is required.");
        }

        GuestRegistrationOrderStartDto response = await mediator.Send(
            new StartGuestRegistrationOrderCommand(
                eventId,
                request.TicketCatalogVersionId,
                request.BookingPartyType,
                request.Lines,
                request.PlatformContributionBasisPoints),
            cancellationToken);

        if (!response.Success)
        {
            return MapGuestStartFailure(response);
        }

        Response.Headers[CapabilityHeader] = response.GuestCapabilityToken;
        Response.Headers.CacheControl = "private, no-store";
        return CreatedAtRoute(
            RouteNames.GetGuestRegistrationOrder,
            new { eventId, orderId = response.Id },
            response);
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.PublicTransactional)]
    [EnableRateLimiting(RateLimitingExtensions.PublicTransactionalPolicy)]
    [RequireIdempotencyKey]
    [HttpPost("guest/{orderId:guid}/attempts", Name = RouteNames.LaunchGuestNativeRegistrationAttempt)]
    [EndpointSummary("Launch guest native registration attempt")]
    [ProducesResponseType(typeof(HalResource<NativeRegistrationAttemptDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<NativeRegistrationAttemptDto>>> LaunchGuestNativeAttempt(
        Guid eventId,
        Guid orderId,
        [FromHeader(Name = CapabilityHeader)] string? capability,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        [FromBody] LaunchNativeRegistrationAttemptRequest request,
        CancellationToken cancellationToken = default) => await LaunchNativeAttempt(
        await mediator.Send(new LaunchGuestNativeRegistrationAttemptCommand(
            eventId, orderId, capability, request.RequirementId, request.ChannelId,
            request.FormId, request.FormVersionId), cancellationToken),
        eventId,
        orderId,
        guest: true);

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [PrivateNoStore]
    [HttpGet("guest/{orderId:guid}/requirement-progress", Name = RouteNames.GetGuestNativeRegistrationRequirementProgress)]
    [EndpointSummary("Get guest native registration requirement progress")]
    [ProducesResponseType(typeof(HalResource<NativeRegistrationRequirementProgressCollectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<NativeRegistrationRequirementProgressCollectionDto>>> GetGuestNativeRequirementProgress(
        Guid eventId,
        Guid orderId,
        [FromHeader(Name = CapabilityHeader)] string? capability,
        CancellationToken cancellationToken = default) => ToNativeProgressResource(
        await mediator.Send(new GetGuestNativeRegistrationRequirementProgressQuery(
            eventId, orderId, capability), cancellationToken), eventId, orderId, guest: true);

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.PublicTransactional)]
    [EnableRateLimiting(RateLimitingExtensions.PublicTransactionalPolicy)]
    [RequireIdempotencyKey]
    [HttpPost("guest/{orderId:guid}/attempts/{attemptId:guid}/skip", Name = RouteNames.SkipGuestNativeRegistrationRequirement)]
    [EndpointSummary("Skip guest optional native registration requirement")]
    [ProducesResponseType(typeof(NativeRegistrationSkipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NativeRegistrationSkipDto>> SkipGuestNativeRequirement(
        Guid eventId,
        Guid orderId,
        Guid attemptId,
        [FromHeader(Name = CapabilityHeader)] string? capability,
        [FromHeader(Name = AttemptCapabilityHeader)] string? attemptCapability,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        [FromBody] SkipNativeRegistrationRequirementRequest request,
        CancellationToken cancellationToken = default) => MapNativeSkip(await mediator.Send(
        new SkipGuestNativeRegistrationRequirementCommand(
            eventId, orderId, capability, request.RequirementId, attemptId, attemptCapability), cancellationToken));

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.PublicTransactional)]
    [EnableRateLimiting(RateLimitingExtensions.PublicTransactionalPolicy)]
    [RequireIdempotencyKey]
    [HttpPost("guest/{orderId:guid}/attempts/{attemptId:guid}/submissions", Name = RouteNames.SubmitGuestNativeRegistrationAttempt)]
    [EndpointSummary("Submit guest native registration answers")]
    [ProducesResponseType(typeof(NativeRegistrationSubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NativeRegistrationSubmissionDto>> SubmitGuestNativeAttempt(
        Guid eventId,
        Guid orderId,
        Guid attemptId,
        [FromHeader(Name = CapabilityHeader)] string? capability,
        [FromHeader(Name = AttemptCapabilityHeader)] string? attemptCapability,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        [FromBody] SubmitNativeRegistrationAttemptRequest request,
        CancellationToken cancellationToken = default) => MapNativeSubmission(await mediator.Send(
        new SubmitGuestNativeRegistrationAttemptCommand(
            eventId, orderId, capability, request.RequirementId, attemptId, attemptCapability,
            idempotencyKey, MapNativeAnswers(request.Answers)), cancellationToken));

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [PrivateNoStore]
    [HttpGet("guest/{orderId:guid}", Name = RouteNames.GetGuestRegistrationOrder)]
    [EndpointSummary("Get guest registration order")]
    [EndpointDescription("Returns a registration order only when the route event, order, and opaque capability header match.")]
    [ProducesResponseType(typeof(HalResource<GuestRegistrationOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<GuestRegistrationOrderDto>>> GetGuest(
        Guid eventId,
        Guid orderId,
        [FromHeader(Name = CapabilityHeader)] string? capability,
        CancellationToken cancellationToken = default)
    {
        GuestRegistrationOrderDto? response = await mediator.Send(
            new GetGuestRegistrationOrderQuery(eventId, orderId, capability),
            cancellationToken);
        return response is null ? this.ToNotFoundProblem(RegistrationOrderNotFoundProblem) : Ok(ToGuestHalResource(response));
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.Public)]
    [PrivateNoStore]
    [HttpGet("guest/{orderId:guid}/participants", Name = RouteNames.GetGuestRegistrationOrderParticipants)]
    [EndpointSummary("Get guest registration participants")]
    [ProducesResponseType(typeof(HalResource<RegistrationOrderParticipantsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<RegistrationOrderParticipantsDto>>> GetGuestParticipants(
        Guid eventId,
        Guid orderId,
        [FromHeader(Name = CapabilityHeader)] string? capability,
        CancellationToken cancellationToken = default)
    {
        RegistrationOrderParticipantsDto? response = await mediator.Send(
            new GetGuestRegistrationOrderParticipantsQuery(eventId, orderId, capability), cancellationToken);
        return response is null
            ? this.ToNotFoundProblem(RegistrationOrderNotFoundProblem)
            : Ok(ToParticipantsHalResource(response, eventId, guest: true));
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.PublicTransactional)]
    [EnableRateLimiting(RateLimitingExtensions.PublicTransactionalPolicy)]
    [RequireIdempotencyKey]
    [HttpPost("guest/{orderId:guid}/participants", Name = RouteNames.AddGuestRegistrationOrderParticipant)]
    [EndpointSummary("Add guest registration participant")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> AddGuestParticipant(
        Guid eventId, Guid orderId, [FromHeader(Name = CapabilityHeader)] string? capability,
        [FromBody] RegistrationParticipantRequest request, CancellationToken cancellationToken = default) =>
        MutateGuest(eventId, orderId, capability,
            new AddRegistrationParticipantCommand(orderId, request.ParticipantTypeId, request.GuardianParticipantId, request.Details),
            cancellationToken);

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.PublicTransactional)]
    [EnableRateLimiting(RateLimitingExtensions.PublicTransactionalPolicy)]
    [RequireIdempotencyKey]
    [HttpPut("guest/{orderId:guid}/participants/{participantId:guid}", Name = RouteNames.UpdateGuestRegistrationOrderParticipant)]
    [EndpointSummary("Update guest registration participant")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> UpdateGuestParticipant(
        Guid eventId, Guid orderId, Guid participantId, [FromHeader(Name = CapabilityHeader)] string? capability,
        [FromBody] RegistrationParticipantRequest request, CancellationToken cancellationToken = default) =>
        MutateGuest(eventId, orderId, capability,
            new UpdateRegistrationParticipantCommand(orderId, participantId, request.ParticipantTypeId, request.GuardianParticipantId, request.Details),
            cancellationToken);

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.PublicTransactional)]
    [EnableRateLimiting(RateLimitingExtensions.PublicTransactionalPolicy)]
    [RequireIdempotencyKey]
    [HttpPut("guest/{orderId:guid}/assignments", Name = RouteNames.AssignGuestRegistrationOrderTickets)]
    [EndpointSummary("Assign guest registration tickets")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> AssignGuestTickets(
        Guid eventId, Guid orderId, [FromHeader(Name = CapabilityHeader)] string? capability,
        [FromBody] RegistrationTicketAssignmentsRequest request, CancellationToken cancellationToken = default) =>
        MutateGuest(eventId, orderId, capability,
            new BulkAssignRegistrationTicketsCommand(orderId, request.Assignments), cancellationToken);

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.PublicTransactional)]
    [EnableRateLimiting(RateLimitingExtensions.PublicTransactionalPolicy)]
    [RequireIdempotencyKey]
    [HttpPut("guest/{orderId:guid}/assignments/deferred", Name = RouteNames.DeferGuestRegistrationOrderTickets)]
    [EndpointSummary("Defer guest registration ticket assignments")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> DeferGuestTickets(
        Guid eventId, Guid orderId, [FromHeader(Name = CapabilityHeader)] string? capability,
        [FromBody] RegistrationTicketDeferralsRequest request, CancellationToken cancellationToken = default) =>
        MutateGuest(eventId, orderId, capability,
            new BulkDeferRegistrationTicketsCommand(orderId, request.Assignments, request.AssignmentDeadline), cancellationToken);

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [PrivateNoStore]
    [HttpGet("", Name = RouteNames.GetEventRegistrationOrders)]
    [EndpointSummary("Get event registration orders")]
    [EndpointDescription("Returns registration orders for one event after event-scoped registration-management authorization.")]
    [ProducesResponseType(typeof(HalCollectionResource<RegistrationOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HalCollectionResource<RegistrationOrderDto>>> GetEventOrders(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RegistrationOrderDto> orders = await mediator.Send(
            new GetEventRegistrationOrdersQuery(eventId),
            cancellationToken);
        HalCollectionResource<RegistrationOrderDto> resource = await assembler.ToCollectionResource(
            orders,
            RouteNames.GetEventRegistrationOrders,
            new { eventId },
            HttpContext);
        return Ok(resource);
    }

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.PublicTransactional)]
    [EnableRateLimiting(RateLimitingExtensions.PublicTransactionalPolicy)]
    [RequireIdempotencyKey]
    [HttpPost("guest/{orderId:guid}/continue", Name = RouteNames.ContinueGuestRegistrationOrder)]
    [EndpointSummary("Continue guest registration order")]
    [EndpointDescription("Advances the guest order only when its opaque capability header matches the scoped route.")]
    [ProducesResponseType(typeof(GuestRegistrationOrderLifecycleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GuestRegistrationOrderLifecycleResponseDto>> ContinueGuest(
        Guid eventId,
        Guid orderId,
        [FromHeader(Name = CapabilityHeader)] string? capability,
        [FromBody] ContinueRegistrationOrderRequest? request = null,
        CancellationToken cancellationToken = default) =>
        MapGuestLifecycle(await mediator.Send(
            new ContinueGuestRegistrationOrderCommand(
                eventId,
                orderId,
                capability,
                request?.PlatformContributionBasisPoints),
            cancellationToken));

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.PublicTransactional)]
    [EnableRateLimiting(RateLimitingExtensions.PublicTransactionalPolicy)]
    [RequireIdempotencyKey]
    [HttpPost("guest/{orderId:guid}/finalize", Name = RouteNames.FinalizeGuestRegistrationOrder)]
    [EndpointSummary("Finalize guest registration order")]
    [EndpointDescription("Finalizes a free guest registration order only when its opaque capability header matches the scoped route.")]
    [ProducesResponseType(typeof(GuestRegistrationOrderLifecycleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GuestRegistrationOrderLifecycleResponseDto>> FinalizeGuest(
        Guid eventId,
        Guid orderId,
        [FromHeader(Name = CapabilityHeader)] string? capability,
        CancellationToken cancellationToken = default) =>
        MapGuestLifecycle(await mediator.Send(
            new FinalizeGuestRegistrationOrderCommand(eventId, orderId, capability),
            cancellationToken));

    [AllowAnonymous]
    [EndpointClassification(EndpointClass.PublicTransactional)]
    [EnableRateLimiting(RateLimitingExtensions.PublicTransactionalPolicy)]
    [RequireIdempotencyKey]
    [HttpDelete("guest/{orderId:guid}", Name = RouteNames.CancelGuestRegistrationOrder)]
    [EndpointSummary("Cancel guest registration order")]
    [EndpointDescription("Cancels a guest registration order only when its opaque capability header matches the scoped route.")]
    [ProducesResponseType(typeof(GuestRegistrationOrderLifecycleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<GuestRegistrationOrderLifecycleResponseDto>> CancelGuest(
        Guid eventId,
        Guid orderId,
        [FromHeader(Name = CapabilityHeader)] string? capability,
        CancellationToken cancellationToken = default) =>
        MapGuestLifecycle(await mediator.Send(
            new CancelGuestRegistrationOrderCommand(eventId, orderId, capability),
            cancellationToken));

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [HttpPost("", Name = RouteNames.StartAuthenticatedRegistrationOrder)]
    [EndpointSummary("Start authenticated registration order")]
    [EndpointDescription("Creates a registration order owned by the authenticated current account.")]
    [Consumes(HateoasConstants.JsonMediaType)]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseCommandResponse<Guid>>> StartAuthenticated(
        Guid eventId,
        [FromBody] StartRegistrationOrderRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return this.ToValidationProblem(RegistrationOrderValidationProblem, "A registration-order payload is required.");
        }

        BaseCommandResponse<Guid> response = await mediator.Send(
            new StartAuthenticatedRegistrationOrderCommand(
                eventId,
                request.TicketCatalogVersionId,
                request.BookingPartyType,
                request.Lines,
                request.PlatformContributionBasisPoints),
            cancellationToken);
        return response.Success
            ? CreatedAtRoute(RouteNames.GetCurrentRegistrationOrder, new { eventId, orderId = response.Id }, response)
            : MapAuthenticatedStartFailure(response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequireIdempotencyKey]
    [HttpPost("{orderId:guid}/attempts", Name = RouteNames.LaunchAuthenticatedNativeRegistrationAttempt)]
    [EndpointSummary("Launch authenticated native registration attempt")]
    [ProducesResponseType(typeof(HalResource<NativeRegistrationAttemptDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<NativeRegistrationAttemptDto>>> LaunchAuthenticatedNativeAttempt(
        Guid eventId,
        Guid orderId,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        [FromBody] LaunchNativeRegistrationAttemptRequest request,
        CancellationToken cancellationToken = default) => await LaunchNativeAttempt(
        await mediator.Send(new LaunchAuthenticatedNativeRegistrationAttemptCommand(
            eventId, orderId, request.RequirementId, request.ChannelId,
            request.FormId, request.FormVersionId), cancellationToken),
        eventId,
        orderId,
        guest: false);

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [PrivateNoStore]
    [HttpGet("{orderId:guid}/requirement-progress", Name = RouteNames.GetAuthenticatedNativeRegistrationRequirementProgress)]
    [EndpointSummary("Get authenticated native registration requirement progress")]
    [ProducesResponseType(typeof(HalResource<NativeRegistrationRequirementProgressCollectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<NativeRegistrationRequirementProgressCollectionDto>>> GetAuthenticatedNativeRequirementProgress(
        Guid eventId,
        Guid orderId,
        CancellationToken cancellationToken = default) => ToNativeProgressResource(
        await mediator.Send(new GetAuthenticatedNativeRegistrationRequirementProgressQuery(
            eventId, orderId), cancellationToken), eventId, orderId, guest: false);

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequireIdempotencyKey]
    [HttpPost("{orderId:guid}/attempts/{attemptId:guid}/skip", Name = RouteNames.SkipAuthenticatedNativeRegistrationRequirement)]
    [EndpointSummary("Skip authenticated optional native registration requirement")]
    [ProducesResponseType(typeof(NativeRegistrationSkipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NativeRegistrationSkipDto>> SkipAuthenticatedNativeRequirement(
        Guid eventId,
        Guid orderId,
        Guid attemptId,
        [FromHeader(Name = AttemptCapabilityHeader)] string? attemptCapability,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        [FromBody] SkipNativeRegistrationRequirementRequest request,
        CancellationToken cancellationToken = default) => MapNativeSkip(await mediator.Send(
        new SkipAuthenticatedNativeRegistrationRequirementCommand(
            eventId, orderId, request.RequirementId, attemptId, attemptCapability), cancellationToken));

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequireIdempotencyKey]
    [HttpPost("{orderId:guid}/attempts/{attemptId:guid}/submissions", Name = RouteNames.SubmitAuthenticatedNativeRegistrationAttempt)]
    [EndpointSummary("Submit authenticated native registration answers")]
    [ProducesResponseType(typeof(NativeRegistrationSubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NativeRegistrationSubmissionDto>> SubmitAuthenticatedNativeAttempt(
        Guid eventId,
        Guid orderId,
        Guid attemptId,
        [FromHeader(Name = AttemptCapabilityHeader)] string? attemptCapability,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        [FromBody] SubmitNativeRegistrationAttemptRequest request,
        CancellationToken cancellationToken = default) => MapNativeSubmission(await mediator.Send(
        new SubmitAuthenticatedNativeRegistrationAttemptCommand(
            eventId, orderId, request.RequirementId, attemptId, attemptCapability,
            idempotencyKey, MapNativeAnswers(request.Answers)), cancellationToken));

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [PrivateNoStore]
    [HttpGet("{orderId:guid}", Name = RouteNames.GetCurrentRegistrationOrder)]
    [EndpointSummary("Get current registration order")]
    [EndpointDescription("Returns a registration order only to its authenticated current-account owner.")]
    [ProducesResponseType(typeof(HalResource<RegistrationOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<RegistrationOrderDto>>> GetCurrent(
        Guid eventId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        RegistrationOrderDto? response = await mediator.Send(new GetCurrentRegistrationOrderQuery(orderId), cancellationToken);
        if (response is null || response.EventId != eventId)
        {
            return this.ToNotFoundProblem(RegistrationOrderNotFoundProblem);
        }

        return await ToHalResource(response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.AuthenticatedPolicy)]
    [PrivateNoStore]
    [HttpGet("{orderId:guid}/participants", Name = RouteNames.GetAuthenticatedRegistrationOrderParticipants)]
    [EndpointSummary("Get registration participants")]
    [ProducesResponseType(typeof(HalResource<RegistrationOrderParticipantsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HalResource<RegistrationOrderParticipantsDto>>> GetAuthenticatedParticipants(
        Guid eventId, Guid orderId, CancellationToken cancellationToken = default)
    {
        RegistrationOrderParticipantsDto? response = await mediator.Send(
            new GetAuthenticatedRegistrationOrderParticipantsQuery(eventId, orderId), cancellationToken);
        return response is null
            ? this.ToNotFoundProblem(RegistrationOrderNotFoundProblem)
            : Ok(ToParticipantsHalResource(response, eventId, guest: false));
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequireIdempotencyKey]
    [HttpPost("{orderId:guid}/participants", Name = RouteNames.AddAuthenticatedRegistrationOrderParticipant)]
    [EndpointSummary("Add registration participant")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> AddAuthenticatedParticipant(
        Guid eventId, Guid orderId, [FromBody] RegistrationParticipantRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAuthenticated(eventId, orderId,
            new AddRegistrationParticipantCommand(orderId, request.ParticipantTypeId, request.GuardianParticipantId, request.Details),
            cancellationToken);

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequireIdempotencyKey]
    [HttpPut("{orderId:guid}/participants/{participantId:guid}", Name = RouteNames.UpdateAuthenticatedRegistrationOrderParticipant)]
    [EndpointSummary("Update registration participant")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> UpdateAuthenticatedParticipant(
        Guid eventId, Guid orderId, Guid participantId, [FromBody] RegistrationParticipantRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAuthenticated(eventId, orderId,
            new UpdateRegistrationParticipantCommand(orderId, participantId, request.ParticipantTypeId, request.GuardianParticipantId, request.Details),
            cancellationToken);

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequireIdempotencyKey]
    [HttpPut("{orderId:guid}/assignments", Name = RouteNames.AssignAuthenticatedRegistrationOrderTickets)]
    [EndpointSummary("Assign registration tickets")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> AssignAuthenticatedTickets(
        Guid eventId, Guid orderId, [FromBody] RegistrationTicketAssignmentsRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAuthenticated(eventId, orderId,
            new BulkAssignRegistrationTicketsCommand(orderId, request.Assignments), cancellationToken);

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [RequireIdempotencyKey]
    [HttpPut("{orderId:guid}/assignments/deferred", Name = RouteNames.DeferAuthenticatedRegistrationOrderTickets)]
    [EndpointSummary("Defer registration ticket assignments")]
    [ProducesResponseType(typeof(BaseCommandResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<BaseCommandResponse<Guid>>> DeferAuthenticatedTickets(
        Guid eventId, Guid orderId, [FromBody] RegistrationTicketDeferralsRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAuthenticated(eventId, orderId,
            new BulkDeferRegistrationTicketsCommand(orderId, request.Assignments, request.AssignmentDeadline), cancellationToken);

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [HttpPost("{orderId:guid}/continue", Name = RouteNames.ContinueAuthenticatedRegistrationOrder)]
    [EndpointSummary("Continue authenticated registration order")]
    [EndpointDescription("Advances a registration order owned by the authenticated current account.")]
    [ProducesResponseType(typeof(HalResource<RegistrationOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HalResource<RegistrationOrderDto>>> ContinueAuthenticated(
        Guid eventId,
        Guid orderId,
        [FromBody] ContinueRegistrationOrderRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        RegistrationOrderLifecycleResponseDto response = await mediator.Send(
            new ContinueAuthenticatedRegistrationOrderCommand(
                eventId,
                orderId,
                request?.PlatformContributionBasisPoints),
            cancellationToken);
        return await MapAuthenticatedLifecycle(eventId, response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [HttpPost("{orderId:guid}/finalize", Name = RouteNames.FinalizeAuthenticatedRegistrationOrder)]
    [EndpointSummary("Finalize authenticated registration order")]
    [EndpointDescription("Finalizes a free registration order owned by the authenticated current account.")]
    [ProducesResponseType(typeof(HalResource<RegistrationOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HalResource<RegistrationOrderDto>>> FinalizeAuthenticated(
        Guid eventId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        RegistrationOrderLifecycleResponseDto response = await mediator.Send(
            new FinalizeAuthenticatedRegistrationOrderCommand(eventId, orderId),
            cancellationToken);
        return await MapAuthenticatedLifecycle(eventId, response);
    }

    [Authorize]
    [EndpointClassification(EndpointClass.Authenticated)]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [HttpDelete("{orderId:guid}", Name = RouteNames.CancelAuthenticatedRegistrationOrder)]
    [EndpointSummary("Cancel authenticated registration order")]
    [EndpointDescription("Cancels a registration order owned by the authenticated current account.")]
    [ProducesResponseType(typeof(HalResource<RegistrationOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HalResource<RegistrationOrderDto>>> CancelAuthenticated(
        Guid eventId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        RegistrationOrderLifecycleResponseDto response = await mediator.Send(
            new CancelAuthenticatedRegistrationOrderCommand(eventId, orderId),
            cancellationToken);
        return await MapAuthenticatedLifecycle(eventId, response);
    }

    private ActionResult<GuestRegistrationOrderStartDto> MapGuestStartFailure(GuestRegistrationOrderStartDto response) =>
        response.FailureCode switch
        {
            "registration_order_identity_required" => this.ToAuthenticationRequiredProblem(
                "Authentication required",
                "An authenticated account is required to start this registration."),
            "registration_order_not_found" => this.ToNotFoundProblem(RegistrationOrderNotFoundProblem),
            _ => this.ToCommandValidationProblem(response, RegistrationOrderValidationProblem)
        };

    private Task<ActionResult<HalResource<NativeRegistrationAttemptDto>>> LaunchNativeAttempt(
        NativeRegistrationAttemptResult response,
        Guid eventId,
        Guid orderId,
        bool guest)
    {
        if (!response.Success || response.Form is null || string.IsNullOrWhiteSpace(response.AttemptCapabilityToken))
        {
            return Task.FromResult<ActionResult<HalResource<NativeRegistrationAttemptDto>>>(
                this.ToNotFoundProblem(RegistrationOrderNotFoundProblem));
        }

        Response.Headers[AttemptCapabilityHeader] = response.AttemptCapabilityToken;
        Response.Headers.CacheControl = "private, no-store";
        var dto = new NativeRegistrationAttemptDto(
            response.AttemptId, response.RequirementId, response.ChannelId, response.FormId, response.FormVersionId,
            response.ExpiresAt, response.AttemptCapabilityToken, response.Form, response.Subjects, response.Progress!);
        string routeName = guest
            ? RouteNames.SubmitGuestNativeRegistrationAttempt
            : RouteNames.SubmitAuthenticatedNativeRegistrationAttempt;
        string progressRouteName = guest
            ? RouteNames.GetGuestNativeRegistrationRequirementProgress
            : RouteNames.GetAuthenticatedNativeRegistrationRequirementProgress;
        var resource = new HalResource<NativeRegistrationAttemptDto>(dto)
            .WithLink(LinkRelations.Submit, HalLink.CreateAction(
                Url.Link(routeName, new { eventId, orderId, attemptId = response.AttemptId })!, HttpMethods.Post))
            .WithLink(LinkRelations.RequirementProgress, HalLink.Create(
                Url.Link(progressRouteName, new { eventId, orderId })!));
        if (response.CanSkip)
        {
            string skipRouteName = guest
                ? RouteNames.SkipGuestNativeRegistrationRequirement
                : RouteNames.SkipAuthenticatedNativeRegistrationRequirement;
            resource.WithLink(LinkRelations.Skip, HalLink.CreateAction(
                Url.Link(skipRouteName, new { eventId, orderId, attemptId = response.AttemptId })!, HttpMethods.Post));
        }

        return Task.FromResult<ActionResult<HalResource<NativeRegistrationAttemptDto>>>(
            CreatedAtRoute(routeName, new { eventId, orderId, attemptId = response.AttemptId }, resource));
    }

    private ActionResult<HalResource<NativeRegistrationRequirementProgressCollectionDto>> ToNativeProgressResource(
        NativeRegistrationRequirementProgressCollectionDto? progress,
        Guid eventId,
        Guid orderId,
        bool guest)
    {
        if (progress is null)
        {
            return this.ToNotFoundProblem(RegistrationOrderNotFoundProblem);
        }

        string selfRouteName = guest
            ? RouteNames.GetGuestNativeRegistrationRequirementProgress
            : RouteNames.GetAuthenticatedNativeRegistrationRequirementProgress;
        string launchRouteName = guest
            ? RouteNames.LaunchGuestNativeRegistrationAttempt
            : RouteNames.LaunchAuthenticatedNativeRegistrationAttempt;
        var routeValues = new { eventId, orderId };
        return Ok(new HalResource<NativeRegistrationRequirementProgressCollectionDto>(progress)
            .WithLink(LinkRelations.Self, HalLink.Create(Url.Link(selfRouteName, routeValues)!))
            .WithLink(LinkRelations.LaunchAttempt, HalLink.CreateAction(
                Url.Link(launchRouteName, routeValues)!, HttpMethods.Post)));
    }

    private ActionResult<NativeRegistrationSkipDto> MapNativeSkip(NativeRegistrationSkipResult response) =>
        response.Success && response.Progress is not null
            ? Ok(new NativeRegistrationSkipDto(response.Progress))
            : this.ToNotFoundProblem(RegistrationOrderNotFoundProblem);

    private ActionResult<NativeRegistrationSubmissionDto> MapNativeSubmission(
        NativeRegistrationSubmissionResult response)
    {
        if (response.Success)
        {
            return Ok(new NativeRegistrationSubmissionDto(response.SubmissionId, true));
        }

        if (response.FailureCode != "registration_submission_invalid")
        {
            return this.ToNotFoundProblem(RegistrationOrderNotFoundProblem);
        }

        var errors = response.Issues
            .GroupBy(issue => issue.FieldKey ?? "$")
            .ToDictionary(group => group.Key, group => group.Select(issue => issue.Code).Distinct().ToArray());
        return ValidationProblem(new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Registration submission validation failed",
            Detail = "The submitted answers did not satisfy the pinned registration form."
        });
    }

    private static IReadOnlyList<RegistrationSubmissionAnswerInput> MapNativeAnswers(
        IReadOnlyList<NativeRegistrationSubmissionAnswerRequest> answers) => answers.Select(answer =>
        new RegistrationSubmissionAnswerInput(
            answer.FieldId,
            answer.SubjectType,
            answer.SubjectId,
            answer.TicketAssignmentOrderLineId,
            answer.Value is JsonElement element
                ? element
                : JsonSerializer.SerializeToElement(answer.Value))).ToArray();

    private async Task<ActionResult<BaseCommandResponse<Guid>>> MutateGuest(
        Guid eventId, Guid orderId, string? capability, IRegistrationParticipantMutation mutation,
        CancellationToken cancellationToken) => MapParticipantMutation(await mediator.Send(
            new MutateGuestRegistrationParticipantsCommand(eventId, orderId, capability, mutation), cancellationToken));

    private async Task<ActionResult<BaseCommandResponse<Guid>>> MutateAuthenticated(
        Guid eventId, Guid orderId, IRegistrationParticipantMutation mutation,
        CancellationToken cancellationToken) => MapParticipantMutation(await mediator.Send(
            new MutateAuthenticatedRegistrationParticipantsCommand(eventId, orderId, mutation), cancellationToken));

    private ActionResult<BaseCommandResponse<Guid>> MapParticipantMutation(BaseCommandResponse<Guid> response) =>
        response.Success
            ? Ok(response)
            : response.FailureCode == "registration_order_not_found"
                ? this.ToNotFoundProblem(RegistrationOrderNotFoundProblem)
                : this.ToCommandValidationProblem(response, RegistrationOrderValidationProblem);

    private HalResource<RegistrationOrderParticipantsDto> ToParticipantsHalResource(
        RegistrationOrderParticipantsDto participants, Guid eventId, bool guest)
    {
        string prefix = guest ? "guest/" : string.Empty;
        var values = new { eventId, orderId = participants.RegistrationOrderId };
        string selfRoute = guest
            ? RouteNames.GetGuestRegistrationOrderParticipants
            : RouteNames.GetAuthenticatedRegistrationOrderParticipants;
        var resource = new HalResource<RegistrationOrderParticipantsDto>(participants)
            .WithLink(LinkRelations.Self, HalLink.Create(Url.Link(selfRoute, values)!));
        if (!participants.CanManage)
        {
            return resource;
        }

        resource.WithLink(LinkRelations.AddParticipant, HalLink.CreateAction(
            Url.Link(guest ? RouteNames.AddGuestRegistrationOrderParticipant : RouteNames.AddAuthenticatedRegistrationOrderParticipant, values)!, HttpMethods.Post));
        resource.WithLink(LinkRelations.UpdateParticipant, new HalLink
        {
            Href = $"/api/events/{eventId:D}/registration-orders/{prefix}{participants.RegistrationOrderId:D}/participants/{{participantId}}",
            Templated = true,
            Method = HttpMethods.Put
        });
        resource.WithLink(LinkRelations.AssignTickets, HalLink.CreateAction(
            Url.Link(guest ? RouteNames.AssignGuestRegistrationOrderTickets : RouteNames.AssignAuthenticatedRegistrationOrderTickets, values)!, HttpMethods.Put));
        resource.WithLink(LinkRelations.DeferTickets, HalLink.CreateAction(
            Url.Link(guest ? RouteNames.DeferGuestRegistrationOrderTickets : RouteNames.DeferAuthenticatedRegistrationOrderTickets, values)!, HttpMethods.Put));
        return resource;
    }

    private ActionResult<BaseCommandResponse<Guid>> MapAuthenticatedStartFailure(BaseCommandResponse<Guid> response) =>
        response.FailureCode switch
        {
            "registration_order_authentication_required" => this.ToAuthenticationRequiredProblem(),
            "registration_order_not_found" => this.ToNotFoundProblem(RegistrationOrderNotFoundProblem),
            _ => this.ToCommandValidationProblem(response, RegistrationOrderValidationProblem)
        };

    private ActionResult<GuestRegistrationOrderLifecycleResponseDto> MapGuestLifecycle(
        GuestRegistrationOrderLifecycleResponseDto response) =>
        response.Success
            ? Ok(response)
            : response.FailureCode == "registration_order_not_found" || response.Order is null
                ? this.ToNotFoundProblem(RegistrationOrderNotFoundProblem)
                : this.ToCommandValidationProblem(response, RegistrationOrderValidationProblem);

    private HalResource<GuestRegistrationOrderDto> ToGuestHalResource(GuestRegistrationOrderDto order)
    {
        var values = new { eventId = order.EventId, orderId = order.Id };
        var resource = new HalResource<GuestRegistrationOrderDto>(order)
            .WithLink(LinkRelations.Self, HalLink.Create(Url.Link(RouteNames.GetGuestRegistrationOrder, values)!));

        if (order.StatusCode is "AWAITING_REQUIREMENTS" or "READY_FOR_CHECKOUT")
        {
            resource.WithLink(LinkRelations.Continue, HalLink.CreateAction(Url.Link(RouteNames.ContinueGuestRegistrationOrder, values)!, HttpMethods.Post));
        }

        if (order.StatusCode == "AWAITING_REQUIREMENTS")
        {
            resource.WithLink(LinkRelations.RequirementProgress, HalLink.Create(
                Url.Link(RouteNames.GetGuestNativeRegistrationRequirementProgress, values)!));
        }

        if (order.StatusCode == "READY_FOR_CHECKOUT")
        {
            resource.WithLink(LinkRelations.Finalize, HalLink.CreateAction(Url.Link(RouteNames.FinalizeGuestRegistrationOrder, values)!, HttpMethods.Post));
        }

        if (order.StatusCode is "DRAFT" or "AWAITING_REQUIREMENTS" or "READY_FOR_CHECKOUT" or "AWAITING_PAYMENT" or "AWAITING_APPROVAL")
        {
            resource.WithLink(LinkRelations.Cancel, HalLink.CreateAction(Url.Link(RouteNames.CancelGuestRegistrationOrder, values)!, HttpMethods.Delete));
        }

        return resource;
    }

    private async Task<ActionResult<HalResource<RegistrationOrderDto>>> MapAuthenticatedLifecycle(
        Guid eventId,
        RegistrationOrderLifecycleResponseDto response)
    {
        if (response.Success && response.Order is { } order && order.EventId == eventId)
        {
            return await ToHalResource(order);
        }

        return response.FailureCode == "registration_order_not_found" || response.Order is null || response.Order.EventId != eventId
            ? this.ToNotFoundProblem(RegistrationOrderNotFoundProblem)
            : this.ToCommandValidationProblem(response, RegistrationOrderValidationProblem);
    }

    private async Task<ObjectResult> ToHalResource(RegistrationOrderDto order)
    {
        var result = new ObjectResult(await assembler.ToResource(order, HttpContext))
        {
            StatusCode = StatusCodes.Status200OK
        };
        result.ContentTypes.Add(HateoasConstants.HalJsonMediaType);
        return result;
    }
}
