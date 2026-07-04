// ABOUTME: Support-access BFF endpoints for current-session UX, start, and actor-owned stop.
// ABOUTME: Keeps impersonation session references server-side while streaming API HAL responses to the browser.

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Explore.Application.DTOs.SupportAccess;
using Explore.Application.Hateoas;
using Explore.Blazor.Services;
using Explore.Blazor.Services.Preferences;
using Explore.Domain.Enums;

namespace Explore.Blazor.Extensions;

public static class BffSupportAccessEndpoints
{
    private const string HalJsonContentType = "application/hal+json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter<SupportAccessModeEnum>() }
    };

    public static WebApplication MapSupportAccessEndpoints(this WebApplication app)
    {
        app.MapGet("/bff/support-access/current", HandleCurrentAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();

        app.MapGet("/bff/support-access/tenants/{targetTenantId:guid}/sessions", HandleListSessionsAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();

        app.MapGet("/bff/support-access/tenants/{targetTenantId:guid}/sessions/{sessionId:guid}/audit-events", HandleAuditEventsAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();

        app.MapPost("/bff/support-access/sessions", HandleStartAsync)
            .RequireAuthorization()
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapPost("/bff/support-access/sessions/current/stop", HandleStopCurrentAsync)
            .RequireAuthorization()
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapPost("/bff/support-access/sessions/{sessionId:guid}/force-stop", HandleForceStopAsync)
            .RequireAuthorization()
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> HandleCurrentAsync(
        HttpContext ctx,
        IHttpClientFactory clientFactory,
        IBffSupportAccessSessionStore sessionStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        using var apiClient = clientFactory.CreateClient("BffClient");
        using var response = await apiClient.GetAsync("api/support-access/current", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return BffForwardingResults.Problem(
                response,
                "Could not resolve the current support-access session.",
                "Support access status failed");
        }

        var current = await response.Content.ReadFromJsonAsync<CurrentSupportAccessSessionDto>(
            JsonOptions,
            cancellationToken);
        var bindResult = await BindCurrentSessionAsync(
            ctx,
            sessionStore,
            current,
            loggerFactory,
            cancellationToken);
        if (bindResult is not null)
        {
            return bindResult;
        }

        return Results.Json(current ?? new CurrentSupportAccessSessionDto(), JsonOptions);
    }

    private static async Task<IResult> HandleListSessionsAsync(
        Guid targetTenantId,
        int limit,
        IHttpClientFactory clientFactory,
        CancellationToken cancellationToken)
    {
        using var apiClient = clientFactory.CreateClient("BffClient");
        using var response = await apiClient.GetAsync(
            $"api/support-access/tenants/{targetTenantId:D}/sessions?limit={ClampLimit(limit)}",
            cancellationToken);

        return await BffForwardingResults.ContentOrProblemAsync(
            response,
            "Could not load support-access sessions.",
            "Support access session history failed",
            cancellationToken);
    }

    private static async Task<IResult> HandleAuditEventsAsync(
        Guid targetTenantId,
        Guid sessionId,
        int limit,
        IHttpClientFactory clientFactory,
        CancellationToken cancellationToken)
    {
        using var apiClient = clientFactory.CreateClient("BffClient");
        using var response = await apiClient.GetAsync(
            $"api/support-access/tenants/{targetTenantId:D}/sessions/{sessionId:D}/audit-events?limit={ClampLimit(limit)}",
            cancellationToken);

        return await BffForwardingResults.ContentOrProblemAsync(
            response,
            "Could not load support-access audit events.",
            "Support access audit failed",
            cancellationToken);
    }

    private static async Task<IResult> HandleStartAsync(
        StartSupportAccessSessionRequestDto? request,
        HttpContext ctx,
        IHttpClientFactory clientFactory,
        IBffSupportAccessSessionStore sessionStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.Problem(
                detail: "Support-access start request body is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid support access request");
        }

        using var apiClient = clientFactory.CreateClient("BffClient");
        using var response = await apiClient.PostAsJsonAsync(
            "api/support-access/sessions",
            request,
            JsonOptions,
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return BffForwardingResults.Problem(
                response,
                "Could not start support access.",
                "Support access start failed");
        }

        var resource = ReadSessionResource(responseBody);
        if (resource?.Data is null)
        {
            return InvalidApiSessionResponse();
        }

        var storeResult = await sessionStore.StoreAsync(ctx.User, resource.Data, cancellationToken);
        if (!storeResult.Success)
        {
            loggerFactory.CreateLogger("SupportAccessBff").LogWarning(
                "Could not bind accepted support-access session to BFF store. FailureCode={FailureCode}",
                storeResult.FailureCode);
            return InvalidApiSessionResponse();
        }

        return ApiHalContent(response, responseBody);
    }

    private static async Task<IResult> HandleStopCurrentAsync(
        StopSupportAccessSessionRequestDto? request,
        HttpContext ctx,
        IHttpClientFactory clientFactory,
        IBffSupportAccessSessionStore sessionStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        using var apiClient = clientFactory.CreateClient("BffClient");
        var currentSession = await ResolveCurrentSessionAsync(
            ctx,
            apiClient,
            sessionStore,
            loggerFactory,
            cancellationToken);
        if (currentSession is null)
        {
            return Results.Problem(
                detail: "No active support-access session is available to stop.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Support access is not active");
        }

        using var response = await apiClient.PostAsJsonAsync(
            $"api/support-access/sessions/{currentSession.Id}/stop",
            request ?? new StopSupportAccessSessionRequestDto(),
            JsonOptions,
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return BffForwardingResults.Problem(
                response,
                "Could not stop support access.",
                "Support access stop failed");
        }

        var resource = ReadSessionResource(responseBody);
        if (resource?.Data is null)
        {
            await sessionStore.ClearAsync(ctx.User, cancellationToken);
            return InvalidApiSessionResponse();
        }

        await sessionStore.ClearAsync(ctx.User, cancellationToken);
        return ApiHalContent(response, responseBody);
    }

    private static async Task<IResult> HandleForceStopAsync(
        Guid sessionId,
        ForceStopSupportAccessSessionRequestDto? request,
        HttpContext ctx,
        IHttpClientFactory clientFactory,
        IBffSupportAccessSessionStore sessionStore,
        CancellationToken cancellationToken)
    {
        using var apiClient = clientFactory.CreateClient("BffClient");
        using var response = await apiClient.PostAsJsonAsync(
            $"api/support-access/sessions/{sessionId:D}/force-stop",
            request ?? new ForceStopSupportAccessSessionRequestDto(),
            JsonOptions,
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return BffForwardingResults.Problem(
                response,
                "Could not force-stop support access.",
                "Support access force-stop failed");
        }

        var cached = await sessionStore.ResolveCurrentAsync(cancellationToken);
        if (cached.Success && cached.Session?.SessionId == sessionId)
        {
            await sessionStore.ClearAsync(ctx.User, cancellationToken);
        }

        var resource = ReadSessionResource(responseBody);
        return resource?.Data is null
            ? InvalidApiSessionResponse()
            : ApiHalContent(response, responseBody);
    }

    private static async Task<SupportAccessSessionDto?> ResolveCurrentSessionAsync(
        HttpContext ctx,
        HttpClient apiClient,
        IBffSupportAccessSessionStore sessionStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var cached = await sessionStore.ResolveCurrentAsync(cancellationToken);
        if (cached.Success && cached.Session is not null)
        {
            return new SupportAccessSessionDto
            {
                Id = cached.Session.SessionId,
                TargetTenantId = cached.Session.TargetTenantId,
                ModeId = cached.Session.ModeId,
                AllowsWrites = cached.Session.AllowsWrites,
                ExpiresAtUtc = cached.Session.ExpiresAtUtc,
                IsActive = true
            };
        }

        using var response = await apiClient.GetAsync("api/support-access/current", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var current = await response.Content.ReadFromJsonAsync<CurrentSupportAccessSessionDto>(
            JsonOptions,
            cancellationToken);
        var bindResult = await BindCurrentSessionAsync(
            ctx,
            sessionStore,
            current,
            loggerFactory,
            cancellationToken);

        return bindResult is null && current?.IsActive == true
            ? current.Session
            : null;
    }

    private static async Task<IResult?> BindCurrentSessionAsync(
        HttpContext ctx,
        IBffSupportAccessSessionStore sessionStore,
        CurrentSupportAccessSessionDto? current,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (current?.IsActive != true || current.Session is null)
        {
            await sessionStore.ClearAsync(ctx.User, cancellationToken);
            return null;
        }

        var storeResult = await sessionStore.StoreAsync(ctx.User, current.Session, cancellationToken);
        if (storeResult.Success)
        {
            return null;
        }

        loggerFactory.CreateLogger("SupportAccessBff").LogWarning(
            "Could not bind current support-access session to BFF store. FailureCode={FailureCode}",
            storeResult.FailureCode);
        return InvalidApiSessionResponse();
    }

    private static HalResource<SupportAccessSessionDto>? ReadSessionResource(string responseBody)
    {
        return string.IsNullOrWhiteSpace(responseBody)
            ? null
            : JsonSerializer.Deserialize<HalResource<SupportAccessSessionDto>>(responseBody, JsonOptions);
    }

    private static IResult ApiHalContent(HttpResponseMessage response, string responseBody)
    {
        var contentType = response.Content.Headers.ContentType?.ToString() ?? HalJsonContentType;
        return Results.Content(
            responseBody,
            contentType,
            statusCode: (int)response.StatusCode);
    }

    private static IResult InvalidApiSessionResponse()
    {
        return Results.Problem(
            detail: "Support-access API returned an invalid session response.",
            statusCode: StatusCodes.Status502BadGateway,
            title: "Invalid support access response");
    }

    private static int ClampLimit(int limit) => Math.Clamp(limit <= 0 ? 100 : limit, 1, 250);
}
