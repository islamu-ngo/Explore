// ABOUTME: Storage BFF endpoint: proxies file uploads to pre-signed S3 URLs.
// ABOUTME: Validates content type, file size, and ensures the URL is a legitimate pre-signed URL.

using System.Net.Http.Headers;

namespace Explore.Blazor.Extensions;

public static class BffStorageEndpoints
{
    /// <summary>
    /// Maps the upload-proxy endpoint: POST /bff/storage/upload-proxy.
    /// </summary>
    public static WebApplication MapStorageEndpoints(this WebApplication app)
    {
        app.MapPost("/bff/storage/upload-proxy", HandleStorageUploadProxyAsync)
            .RequireAuthorization()
            .ValidateAntiforgery()
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> HandleStorageUploadProxyAsync(
        HttpContext ctx,
        IHttpClientFactory clientFactory,
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
        var uploadUrl = form["uploadUrl"].ToString();
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

        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var uploadUri) ||
            !string.Equals(uploadUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                detail: "Invalid upload URL.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var query = uploadUri.Query;
        if (!query.Contains("X-Amz-Algorithm", StringComparison.OrdinalIgnoreCase) ||
            !query.Contains("X-Amz-Signature", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Rejected upload proxy request for non-presigned URL host {Host}",
                uploadUri.Host);
            return Results.Problem(
                detail: "Upload URL must be pre-signed.",
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

        try
        {
            using var s3Client = clientFactory.CreateClient("S3Upload");
            await using var stream = file.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.ContentType = mediaTypeHeader;

            using var response = await s3Client.PutAsync(uploadUri, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "Upload proxy failed for host {Host}. Status={StatusCode}, Body={Body}",
                    uploadUri.Host, (int)response.StatusCode, responseBody);

                return Results.Problem(
                    detail: "Storage upload failed.",
                    statusCode: StatusCodes.Status502BadGateway);
            }

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
}
