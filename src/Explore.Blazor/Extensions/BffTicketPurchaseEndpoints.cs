// ABOUTME: Maps antiforgery-protected BFF endpoints for authenticated and capability guest purchases.
// ABOUTME: Creates operation identity server-side and forwards no caller-controlled tenant or quantity facts.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Extensions;

public static class BffTicketPurchaseEndpoints
{
    private const int VerifiedContactAccessMode = 2;
    private const int NameOnlyAccessMode = 3;

    public static WebApplication MapTicketPurchaseEndpoints(
        this WebApplication app)
    {
        app.MapPost(
                "/bff/ticket-purchases/authenticated",
                HandleAuthenticatedAsync)
            .RequireAuthorization()
            .RequireRateLimiting(
                RateLimitingExtensions
                    .TicketPurchaseAuthorityPolicy)
            .ValidateAntiforgeryBeforeRateLimiting();
        app.MapPost(
                "/bff/ticket-purchases/guest",
                HandleGuestAsync)
            .RequireRateLimiting(
                RateLimitingExtensions
                    .TicketPurchaseAuthorityPolicy)
            .ValidateAntiforgeryBeforeRateLimiting();
        return app;
    }

    private static async Task<IResult> HandleAuthenticatedAsync(
        BffTicketPurchaseRequest request,
        IEventApiClient api,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredLineage(request))
        {
            return Results.BadRequest();
        }

        var body = new ReserveTicketPurchaseRequest
        {
            AccessMode = 1,
            RequestedPurchaserActorId =
                request.RequestedPurchaserActorId,
        };
        return await ForwardAsync(
            () => api.ReserveAuthenticatedPurchaseAuthorityAsync(
                request.EventId,
                request.OrderId,
                CreateOperationKey(),
                body: body,
                cancellationToken: cancellationToken));
    }

    private static async Task<IResult> HandleGuestAsync(
        BffTicketPurchaseRequest request,
        IEventApiClient api,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredLineage(request)
            || string.IsNullOrWhiteSpace(
                request.GuestCapability)
            || request.AccessMode is not (
                VerifiedContactAccessMode
                or NameOnlyAccessMode))
        {
            return Results.NotFound();
        }

        var body = new ReserveTicketPurchaseRequest
        {
            AccessMode = request.AccessMode,
            RequestedPurchaserActorId = null,
        };
        return await ForwardAsync(
            () => api.ReserveGuestPurchaseAuthorityAsync(
                request.EventId,
                request.OrderId,
                CreateOperationKey(),
                request.GuestCapability,
                body: body,
                cancellationToken: cancellationToken));
    }

    private static async Task<IResult> ForwardAsync(
        Func<Task<
            HalResourceOfTicketPurchaseGovernanceResource>>
            forward)
    {
        try
        {
            HalResourceOfTicketPurchaseGovernanceResource
                response = await forward();
            return Results.Json(response);
        }
        catch (ApiException exception)
        {
            return exception.StatusCode is
                >= StatusCodes.Status400BadRequest
                and <= StatusCodes.Status429TooManyRequests
                ? Results.StatusCode(exception.StatusCode)
                : Results.StatusCode(
                    StatusCodes.Status502BadGateway);
        }
    }

    private static bool HasRequiredLineage(
        BffTicketPurchaseRequest request) =>
        request.EventId != Guid.Empty
        && request.OrderId != Guid.Empty;

    private static string CreateOperationKey() =>
        Guid.CreateVersion7().ToString("N");
}

public sealed record BffTicketPurchaseRequest
{
    public Guid EventId { get; init; }
    public Guid OrderId { get; init; }
    public int AccessMode { get; init; }
    public Guid? RequestedPurchaserActorId { get; init; }
    public string? GuestCapability { get; init; }
}
