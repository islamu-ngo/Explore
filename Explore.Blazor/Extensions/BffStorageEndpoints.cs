// ABOUTME: Storage BFF endpoints issue upload sessions and proxy files to server-approved destinations.
// ABOUTME: Documented antiforgery exception: InteractiveServer circuit self-calls cannot forward the browser cookie pair.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Responses;
using Explore.Blazor.Services;
using Explore.Domain;

namespace Explore.Blazor.Extensions;

public static class BffStorageEndpoints
{
    private const int MaxUploadFileNameLength = 500;
    private const int MaxUploadContentTypeLength = 100;
    private const int UploadSessionIdLength = 32;
    private static readonly HashSet<string> ReservedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "COM5",
        "COM6",
        "COM7",
        "COM8",
        "COM9",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4",
        "LPT5",
        "LPT6",
        "LPT7",
        "LPT8",
        "LPT9"
    };

    /// <summary>
    /// Maps the storage BFF endpoints: POST /bff/storage/upload-session and POST /bff/storage/upload-proxy.
    /// </summary>
    /// <remarks>
    /// <para><b>Documented antiforgery exception (per docs/SECURITY-MODEL.md §"Internal exception register"):</b></para>
    /// <para>These endpoints do NOT call <c>ValidateAntiforgery()</c>. The Blazor image-upload flow runs under
    /// <c>@rendermode InteractiveServer</c>, so the BFF request is a server-to-self HttpClient call inside the
    /// same host process. The browser antiforgery cookie pair (<c>XSRF-TOKEN</c> cookie + <c>X-CSRF-TOKEN</c>
    /// header) cannot be forwarded reliably from a live SignalR circuit because the original GET that distributed
    /// the token is no longer in scope. This matches the documented constraint in
    /// <c>BffAuthEndpoints.cs</c> ("InteractiveServer self-calls cannot reliably satisfy browser antiforgery
    /// semantics") and the existing pattern in <c>BffSetupSecretEndpoints.cs</c>.</para>
    /// <para><b>Equivalent compensating controls:</b></para>
    /// <list type="number">
    /// <item><c>RequireAuthorization()</c> — only authenticated users may request sessions or proxy uploads.</item>
    /// <item><c>IStorageUploadSessionStore</c> binds every session to the caller's <c>sub</c> claim at issue time
    /// and verifies ownership again at resolve time (<c>session_owner_mismatch</c> failure on mismatch).</item>
    /// <item>Session IDs are 32-char cryptographically random hex (<c>RandomNumberGenerator</c>) — unguessable.</item>
    /// <item>Sessions are short-lived (clamped 15–60 minutes) and stored in <c>IDistributedCache</c>.</item>
    /// <item>Each session accepts a single upload (<c>ConsumeAsync</c> after success) with a fixed
    /// <c>ContentType</c> and <c>ExpectedSizeBytes</c> verified at resolve time.</item>
    /// <item>The proxy rejects caller-supplied <c>uploadUrl</c> and streams the file server-side to the
    /// API session-finalize endpoint — the browser never reaches the storage provider directly.</item>
    /// <item>Per-user rate limiting per <c>RateLimitingExtensions.cs</c> bounds brute-force attempts.</item>
    /// </list>
    /// </remarks>
    public static WebApplication MapStorageEndpoints(this WebApplication app)
    {
        app.MapPost("/bff/storage/upload-session", HandleStorageUploadSessionAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();

        app.MapPost("/bff/storage/upload-proxy", HandleStorageUploadProxyAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> HandleStorageUploadSessionAsync(
        StorageUploadSessionRequest? request,
        HttpContext ctx,
        IHttpClientFactory clientFactory,
        IStorageUploadSessionStore sessionStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("StorageUploadSession");

        var validationProblem = ValidateUploadSessionRequest(request, out var normalizedRequest);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        using var apiClient = clientFactory.CreateClient("BffClient");
        using var response = await apiClient.PostAsJsonAsync("api/storageobject/upload-sessions", normalizedRequest, cancellationToken);
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

        var uploadResponse = await response.Content.ReadFromJsonAsync<BaseCommandResponse<StorageUploadSessionDto>>(cancellationToken);
        if (uploadResponse?.Success != true || uploadResponse.Id is null)
        {
            return Results.Problem(
                detail: "Storage service returned an invalid upload session response.",
            statusCode: StatusCodes.Status502BadGateway);
        }

        var issueResult = await sessionStore.IssueAsync(ctx.User, uploadResponse.Id, normalizedRequest.ContentType, cancellationToken);
        if (!issueResult.Success || string.IsNullOrWhiteSpace(issueResult.SessionId))
        {
            logger.LogWarning(
                "Rejected storage upload session response. FailureCode={FailureCode}",
                issueResult.FailureCode);
            return Results.Problem(
                detail: "Storage service returned an invalid upload session.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(new StorageUploadSessionResponse(
            issueResult.SessionId,
            string.Empty,
            string.Empty,
            issueResult.ExpiresInMinutes));
    }

    private static async Task<IResult> HandleStorageUploadProxyAsync(
        HttpContext ctx,
        IHttpClientFactory clientFactory,
        IStorageUploadSessionStore sessionStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("StorageUploadProxy");

        if (!ctx.Request.HasFormContentType)
        {
            return Results.Problem(
                detail: "Request must be multipart/form-data.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        IFormCollection form;
        try
        {
            form = await ctx.Request.ReadFormAsync(cancellationToken);
        }
        catch (BadHttpRequestException)
        {
            return InvalidStorageUploadRequest("Multipart upload request is invalid.");
        }
        catch (InvalidDataException)
        {
            return InvalidStorageUploadRequest("Multipart upload request is invalid.");
        }

        if (form.ContainsKey("uploadUrl"))
        {
            return InvalidStorageUploadRequest("Raw upload destinations are not accepted.");
        }

        var uploadSessionId = form["uploadSessionId"].ToString().Trim();
        var file = form.Files.GetFile("file");

        if (!IsValidUploadSessionId(uploadSessionId))
        {
            return InvalidStorageUploadRequest("A valid server-issued upload session is required.");
        }

        if (file is null || file.Length == 0)
        {
            return Results.Problem(
                detail: "File is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!IsSafeBrowserFileName(file.FileName, out var fileNameProblem))
        {
            return InvalidStorageUploadRequest(fileNameProblem);
        }

        var declaredContentType = form["contentType"].ToString();
        var candidateContentType = string.IsNullOrWhiteSpace(declaredContentType)
            ? file.ContentType
            : declaredContentType;

        if (!TryNormalizeContentType(candidateContentType, out var contentType, out var mediaTypeHeader, out var contentTypeProblem))
        {
            return InvalidStorageUploadRequest(contentTypeProblem);
        }

        if (!string.IsNullOrWhiteSpace(declaredContentType) &&
            !string.IsNullOrWhiteSpace(file.ContentType) &&
            (!TryNormalizeContentType(file.ContentType, out var fileContentType, out _, out contentTypeProblem) ||
                !string.Equals(contentType, fileContentType, StringComparison.OrdinalIgnoreCase)))
        {
            return InvalidStorageUploadRequest("File content type must match declared content type.");
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

        if (file.Length != resolution.Session.ExpectedSizeBytes)
        {
            return InvalidStorageUploadRequest("File size must match the reserved upload session size.");
        }

        try
        {
            using var apiClient = clientFactory.CreateClient("BffClient");
            await using var stream = file.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.ContentType = mediaTypeHeader;
            content.Headers.ContentLength = file.Length;

            using var response = await apiClient.PutAsync(
                $"api/storageobject/upload-sessions/{resolution.Session.ApiUploadSessionId}/content",
                content,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Upload proxy failed for a resolved API upload session. Status={StatusCode}, HasBody={HasBody}",
                    (int)response.StatusCode,
                    response.Content.Headers.ContentLength.GetValueOrDefault() > 0);

                return Results.Problem(
                    detail: "Storage upload failed.",
                    statusCode: StatusCodes.Status502BadGateway);
            }

            var uploadResponse = await response.Content.ReadFromJsonAsync<BaseCommandResponse<StorageUploadSessionDto>>(cancellationToken);
            if (uploadResponse?.Success != true || uploadResponse.Id?.StorageObjectId is null)
            {
                logger.LogWarning(
                    "Upload proxy received invalid finalization response for a resolved API upload session.");
                return Results.Problem(
                    detail: "Storage upload finalized without storage object metadata.",
                    statusCode: StatusCodes.Status502BadGateway);
            }

            await sessionStore.ConsumeAsync(resolution.Session.SessionId, cancellationToken);
            var storageObjectId = uploadResponse.Id.StorageObjectId.Value;
            var contentUrl = $"/api/storageobject/{storageObjectId}/content";
            var publicUrl = string.Equals(uploadResponse.Id.Visibility, StorageObjectVisibilities.PublicImage, StringComparison.Ordinal)
                ? $"/api/storageobject/{storageObjectId}/public"
                : contentUrl;

            return Results.Ok(new StorageUploadProxyResponse(
                storageObjectId,
                publicUrl,
                contentUrl));
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Upload proxy failed for a resolved API upload session. FailureType={FailureType}",
                CategorizeUploadProxyFailure(ex));
            return Results.Problem(
                detail: "Storage upload failed due to an internal proxy error.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private sealed record StorageUploadSessionRequest(
        string FileName,
        string ContentType,
        long ExpectedSizeBytes);

    private sealed record StorageUploadSessionResponse(
        string UploadSessionId,
        string ObjectKey,
        string ViewUrl,
        int ExpiresInMinutes);

    private sealed record StorageUploadProxyResponse(
        Guid StorageObjectId,
        string ViewUrl,
        string ContentUrl);

    private static IResult? ValidateUploadSessionRequest(StorageUploadSessionRequest? request, out CreateStorageUploadSessionDto normalizedRequest)
    {
        normalizedRequest = new CreateStorageUploadSessionDto
        {
            ExpectedSizeBytes = request?.ExpectedSizeBytes ?? 0,
            ContentType = request?.ContentType?.Trim() ?? string.Empty,
            OriginalFileName = request?.FileName?.Trim(),
            SafeDisplayName = request?.FileName?.Trim(),
            Extension = Path.GetExtension(request?.FileName ?? string.Empty).TrimStart('.'),
            Purpose = StorageObjectPurposes.LegacyImage,
            Visibility = StorageObjectVisibilities.PublicImage,
            IdempotencyKey = $"bff:{Guid.CreateVersion7():N}"
        };

        if (request is null)
        {
            return InvalidStorageUploadRequest("Upload session request body is required.");
        }

        if (normalizedRequest.ExpectedSizeBytes <= 0)
        {
            return InvalidStorageUploadRequest("Expected file size must be greater than zero.");
        }

        if (!IsSafeBrowserFileName(normalizedRequest.OriginalFileName, out var fileNameProblem))
        {
            return InvalidStorageUploadRequest(fileNameProblem);
        }

        if (!TryNormalizeContentType(normalizedRequest.ContentType, out var contentType, out _, out var contentTypeProblem))
        {
            return InvalidStorageUploadRequest(contentTypeProblem);
        }

        normalizedRequest.ContentType = contentType;
        return null;
    }

    private static bool TryNormalizeContentType(
        string? value,
        out string contentType,
        out MediaTypeHeaderValue? mediaTypeHeader,
        out string problem)
    {
        contentType = string.Empty;
        mediaTypeHeader = null;
        problem = string.Empty;

        var candidate = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            problem = "Content type is required.";
            return false;
        }

        if (candidate.Length > MaxUploadContentTypeLength)
        {
            problem = $"Content type must be {MaxUploadContentTypeLength} characters or fewer.";
            return false;
        }

        if (ContainsControlCharacters(candidate) ||
            candidate.Count(static character => character == '/') != 1 ||
            !MediaTypeHeaderValue.TryParse(candidate, out mediaTypeHeader) ||
            string.IsNullOrWhiteSpace(mediaTypeHeader.MediaType) ||
            mediaTypeHeader.MediaType.Count(static character => character == '/') != 1 ||
            mediaTypeHeader.MediaType.Contains('*', StringComparison.Ordinal))
        {
            problem = "Content type must be a valid MIME type.";
            return false;
        }

        contentType = mediaTypeHeader.ToString();
        return true;
    }

    private static bool IsSafeBrowserFileName(string? value, out string problem)
    {
        var fileName = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            problem = "File name is required.";
            return false;
        }

        if (fileName.Length > MaxUploadFileNameLength)
        {
            problem = $"File name must be {MaxUploadFileNameLength} characters or fewer.";
            return false;
        }

        if (ContainsControlCharacters(fileName) ||
            fileName.Contains('/', StringComparison.Ordinal) ||
            fileName.Contains('\\', StringComparison.Ordinal) ||
            string.Equals(fileName, ".", StringComparison.Ordinal) ||
            string.Equals(fileName, "..", StringComparison.Ordinal))
        {
            problem = "File name must be a simple file name without path segments or control characters.";
            return false;
        }

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        if (ReservedFileNames.Contains(baseName))
        {
            problem = "File name must not use a reserved device name.";
            return false;
        }

        problem = string.Empty;
        return true;
    }

    private static bool IsValidUploadSessionId(string sessionId)
    {
        if (sessionId.Length != UploadSessionIdLength)
        {
            return false;
        }

        return sessionId.All(static c =>
            (c >= '0' && c <= '9') ||
            (c >= 'a' && c <= 'f') ||
            (c >= 'A' && c <= 'F'));
    }

    private static bool ContainsControlCharacters(string value) =>
        value.Any(char.IsControl);

    private static string CategorizeUploadProxyFailure(Exception exception) =>
        exception switch
        {
            OperationCanceledException => "canceled",
            HttpRequestException => "api_unavailable",
            IOException => "stream_io",
            InvalidOperationException => "proxy_invalid_operation",
            _ => "unknown"
        };

    private static IResult InvalidStorageUploadRequest(string detail) =>
        Results.Problem(
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid storage upload request");
}
