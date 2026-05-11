// ABOUTME: Storage BFF endpoints issue upload sessions and proxy files to server-approved destinations.
// ABOUTME: Binds browser uploads to exact BFF-created sessions instead of trusting caller-supplied URLs.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Explore.Application.DTOs.StorageObject;
using Explore.Blazor.Services;

namespace Explore.Blazor.Extensions;

public static class BffStorageEndpoints
{
    /// <summary>
    /// Maps the upload-proxy endpoint: POST /bff/storage/upload-proxy.
    /// </summary>
    public static WebApplication MapStorageEndpoints(this WebApplication app)
    {
        app.MapPost("/bff/storage/upload-session", HandleStorageUploadSessionAsync)
            .RequireAuthorization()
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        app.MapPost("/bff/storage/upload-proxy", HandleStorageUploadProxyAsync)
            .RequireAuthorization()
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> HandleStorageUploadSessionAsync(
        UploadRequestDto request,
        HttpContext ctx,
        IHttpClientFactory clientFactory,
        IStorageUploadSessionStore sessionStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("StorageUploadSession");
        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))
        {
            return Results.Problem(
                detail: "File name and content type are required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        using var apiClient = clientFactory.CreateClient("BffClient");
        using var response = await apiClient.PostAsJsonAsync("api/storageobject/generate-upload-url", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Storage upload session generation failed. Status={StatusCode}, HasBody={HasBody}",
                (int)response.StatusCode,
                response.Content.Headers.ContentLength.GetValueOrDefault() > 0);

            return Results.Problem(
                detail: "Failed to create a storage upload session.",
                statusCode: response.StatusCode == HttpStatusCode.Unauthorized
                    ? StatusCodes.Status401Unauthorized
                    : StatusCodes.Status502BadGateway);
        }

        var uploadResponse = await response.Content.ReadFromJsonAsync<UploadUrlResponseDto>(cancellationToken);
        if (uploadResponse is null)
        {
            return Results.Problem(
                detail: "Storage service returned an invalid upload session response.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        var issueResult = await sessionStore.IssueAsync(ctx.User, uploadResponse, request.ContentType, cancellationToken);
        if (!issueResult.Success || string.IsNullOrWhiteSpace(issueResult.SessionId))
        {
            logger.LogWarning("Rejected storage upload session response. FailureCode={FailureCode}", issueResult.FailureCode);
            return Results.Problem(
                detail: "Storage service returned an untrusted upload destination.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(new StorageUploadSessionResponse(
            issueResult.SessionId,
            issueResult.ObjectKey ?? string.Empty,
            issueResult.ViewUrl ?? string.Empty,
            issueResult.ExpiresInMinutes));
    }

    private static async Task<IResult> HandleStorageUploadProxyAsync(
        HttpContext ctx,
        IHttpClientFactory clientFactory,
        IStorageUploadSessionStore sessionStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        const long maxUploadBytes = 10 * 1024 * 1024;
        var logger = loggerFactory.CreateLogger("StorageUploadProxy");

        if (!ctx.Request.HasFormContentType)
        {
            return Results.Problem(
                detail: "Request must be multipart/form-data.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var form = await ctx.Request.ReadFormAsync(cancellationToken);
        var uploadSessionId = form["uploadSessionId"].ToString();
        var contentType = form["contentType"].ToString();
        var file = form.Files.GetFile("file");

        if (file is null || file.Length == 0)
        {
            return Results.Problem(
                detail: "File is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (file.Length > maxUploadBytes)
        {
            return Results.Problem(
                detail: "File exceeds max size (10MB).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;
        }

        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaTypeHeader))
        {
            return Results.Problem(
                detail: "Invalid content type.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var resolution = await sessionStore.ResolveAsync(ctx.User, uploadSessionId, contentType, cancellationToken);
        if (!resolution.Success || resolution.Session is null)
        {
            logger.LogWarning("Rejected upload proxy request without a valid server-issued upload session. FailureCode={FailureCode}",
                resolution.FailureCode);
            return Results.Problem(
                detail: "A valid server-issued upload session is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var uploadUri = new Uri(resolution.Session.UploadUrl, UriKind.Absolute);

        try
        {
            using var s3Client = clientFactory.CreateClient("S3Upload");
            await using var stream = file.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.ContentType = mediaTypeHeader;

            using var response = await s3Client.PutAsync(uploadUri, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Upload proxy failed for bound host {Host}. Status={StatusCode}, HasBody={HasBody}",
                    uploadUri.Host,
                    (int)response.StatusCode,
                    response.Content.Headers.ContentLength.GetValueOrDefault() > 0);

                return Results.Problem(
                    detail: "Storage upload failed.",
                    statusCode: StatusCodes.Status502BadGateway);
            }

            await sessionStore.ConsumeAsync(resolution.Session.SessionId, cancellationToken);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upload proxy exception for host {Host}", uploadUri.Host);
            return Results.Problem(
                detail: "Storage upload failed due to an internal proxy error.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private sealed record StorageUploadSessionResponse(
        string UploadSessionId,
        string ObjectKey,
        string ViewUrl,
        int ExpiresInMinutes);
}
