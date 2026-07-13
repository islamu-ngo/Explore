// ABOUTME: REST API controller for provider operational webhooks that authenticate by signature.
// ABOUTME: Keeps incoming callbacks independent from outgoing Local/Svix provider selection.

using System.Text.Json;
using Asp.Versioning;
using Explore.API.Attributes;
using Explore.API.ExceptionHandling;
using Explore.API.Extensions;
using Explore.API.Hateoas;
using Explore.API.Services;
using Explore.Application.Telemetry;
using Explore.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Explore.API.Controllers;

[ApiVersion("0.1")]
[Route("api/integrations")]
[ApiController]
[EndpointClassification(EndpointClass.Public)]
[Produces(HateoasConstants.JsonMediaType)]
public sealed class IncomingWebhooksController(
    IIncomingWebhookIntakeService incomingWebhookIntakeService,
    IOptionsMonitor<WebhookOptions> webhookOptions,
    BusinessMetrics metrics,
    ILogger<IncomingWebhooksController> logger) : ExploreControllerBase
{
    [HttpPost("svix/operational", Name = RouteNames.IntegrationSvixOperationalCallback)]
    [EndpointSummary("Record Svix Operational Webhook")]
    [EndpointDescription("Accepts signed Svix operational callbacks without requiring the outgoing webhook provider to be enabled.")]
    [AllowAnonymous]
    [Consumes(HateoasConstants.JsonMediaType)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [EnableRateLimiting(RateLimitingExtensions.PublicIngestionPolicy)]
    public async Task<IActionResult> RecordSvixOperationalCallback(
        CancellationToken cancellationToken = default)
    {
        var incoming = await incomingWebhookIntakeService.ReadAndVerifyAsync(
            Request,
            "svix",
            webhookOptions.CurrentValue.Svix.OperationalWebhookMaxBodyBytes,
            cancellationToken);
        if (!incoming.Succeeded)
        {
            metrics.RecordEventReportProviderCallback("svix", "failed", incoming.Code);
            logger.LogWarning(
                "Svix operational webhook rejected with status {StatusCode} failure {FailureCategory}",
                incoming.StatusCode,
                incoming.Code);

            return ApiProblemFactory.ToProblemResult(ApiProblemFactory.CreateProblem(
                HttpContext,
                incoming.StatusCode,
                incoming.Title,
                incoming.Type,
                incoming.Detail,
                incoming.Code));
        }

        var tenantId = TryResolveTenantId(incoming.RawPayload);
        if (tenantId.HasValue)
        {
            var capture = await incomingWebhookIntakeService.CaptureAsync(
                incoming,
                tenantId.Value,
                incoming.Verification?.ProviderMessageId,
                incoming.Verification?.EventType ?? "svix.operational",
                incoming.Verification?.IdempotencyKey,
                cancellationToken);
            if (!capture.Succeeded)
            {
                metrics.RecordEventReportProviderCallback("svix", "failed", capture.Code);
                return ApiProblemFactory.ToProblemResult(ApiProblemFactory.CreateProblem(
                    HttpContext,
                    capture.StatusCode,
                    capture.Title,
                    capture.Type,
                    capture.Detail,
                    capture.Code));
            }
        }

        metrics.RecordEventReportProviderCallback(
            "svix",
            "succeeded",
            "none");
        return Accepted();
    }

    private static Guid? TryResolveTenantId(string? rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (TryGetGuidProperty(document.RootElement, "tenantId", out var tenantId) ||
                TryGetGuidProperty(document.RootElement, "tenant_id", out tenantId))
            {
                return tenantId;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static bool TryGetGuidProperty(JsonElement element, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        return Guid.TryParse(property.GetString(), out value) && value != Guid.Empty;
    }
}
