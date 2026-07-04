// ABOUTME: Safe Minimal API result translators for BFF-to-API forwarding responses.
// ABOUTME: Centralizes generic error bodies while preserving endpoint-owned auth and antiforgery checks.

namespace Explore.Blazor.Services.Preferences;

public static class BffForwardingResults
{
    public static async Task<IResult> JsonStreamOrFallbackAsync<TFallback>(
        HttpResponseMessage response,
        TFallback fallback,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return Results.Ok(fallback);
        }

        return await JsonContentAsync(response, cancellationToken);
    }

    public static async Task<IResult> JsonStreamOrProblemAsync(
        HttpResponseMessage response,
        string failureDetail,
        string failureTitle,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return Problem(response, failureDetail, failureTitle);
        }

        return await JsonContentAsync(response, cancellationToken);
    }

    public static IResult OkOrProblem(
        HttpResponseMessage response,
        string failureDetail,
        string failureTitle)
    {
        return response.IsSuccessStatusCode
            ? Results.Ok()
            : Problem(response, failureDetail, failureTitle);
    }

    public static async Task<IResult> ContentOrProblemAsync(
        HttpResponseMessage response,
        string failureDetail,
        string failureTitle,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return Problem(response, failureDetail, failureTitle);
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return Results.Content(payload, response.Content.Headers.ContentType?.MediaType ?? "application/json");
    }

    public static IResult? ProblemOrNull(
        HttpResponseMessage response,
        string failureDetail,
        string failureTitle)
    {
        return response.IsSuccessStatusCode
            ? null
            : Problem(response, failureDetail, failureTitle);
    }

    public static IResult Problem(HttpResponseMessage response, string failureDetail, string failureTitle)
    {
        return Results.Problem(
            detail: failureDetail,
            statusCode: (int)response.StatusCode,
            title: failureTitle);
    }

    private static async Task<IResult> JsonContentAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return Results.Content(payload, response.Content.Headers.ContentType?.MediaType ?? "application/json");
    }
}
